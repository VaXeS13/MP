import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { ItemCheckoutService } from '../proxy/application/sellers/item-checkout.service';
import {
  ItemForCheckoutDto,
  AvailablePaymentMethodsDto,
  CheckoutSummaryDto,
} from '../proxy/application/contracts/sellers/models';
import { PaymentMethodType } from '../proxy/application/contracts/sellers/payment-method-type.enum';

@Component({
  selector: 'app-seller-checkout',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    Card,
    InputText,
    Message,
    ProgressSpinner,
    Toast,
    ConfirmDialog,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './seller-checkout.component.html',
  styleUrl: './seller-checkout.component.scss',
})
export class SellerCheckoutComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  barcode = '';
  scannedItems: ItemForCheckoutDto[] = [];
  checkoutSummary: CheckoutSummaryDto | null = null;
  availablePaymentMethods: AvailablePaymentMethodsDto | null = null;
  isLoading = false;
  isProcessing = false;
  showPaymentDialog = false;

  PaymentMethodType = PaymentMethodType;

  constructor(
    private itemCheckoutService: ItemCheckoutService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService
  ) {}

  ngOnInit(): void {
    this.loadAvailablePaymentMethods();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAvailablePaymentMethods(): void {
    this.itemCheckoutService
      .getAvailablePaymentMethods()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.availablePaymentMethods = result;
        },
        error: error => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load payment methods',
          });
        },
      });
  }

  onBarcodeInput(event: KeyboardEvent): void {
    if (event.key === 'Enter' && this.barcode.trim()) {
      this.findItem();
    }
  }

  findItem(): void {
    if (!this.barcode.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please scan or enter a barcode',
      });
      return;
    }

    this.isLoading = true;

    this.itemCheckoutService
      .findItemByBarcode({ barcode: this.barcode.trim() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.isLoading = false;

          if (result) {
            // Check if item already scanned
            if (this.scannedItems.find(x => x.id === result.id)) {
              this.messageService.add({
                severity: 'warn',
                summary: 'Already Scanned',
                detail: 'This item is already in the list',
              });
            } else if (result.status !== 'ForSale') {
              this.messageService.add({
                severity: 'warn',
                summary: 'Item Not Available',
                detail: `This item is ${result.status} and cannot be sold`,
              });
            } else {
              this.scannedItems.push(result);
              this.calculateSummary();
              this.messageService.add({
                severity: 'success',
                summary: 'Item Added',
                detail: `${result.name} added to list`,
              });
            }
          } else {
            this.messageService.add({
              severity: 'info',
              summary: 'Not Found',
              detail: 'No item found with this barcode',
            });
          }

          this.barcode = '';
        },
        error: error => {
          this.isLoading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to find item',
          });
        },
      });
  }

  private calculateSummary(): void {
    if (this.scannedItems.length === 0) {
      this.checkoutSummary = null;
      return;
    }

    this.checkoutSummary = {
      items: [...this.scannedItems],
      itemsCount: this.scannedItems.length,
      totalAmount: this.scannedItems.reduce((sum, item) => sum + (item.actualPrice || 0), 0),
      totalCommission: this.scannedItems.reduce((sum, item) => sum + (item.commissionAmount || 0), 0),
      totalCustomerAmount: this.scannedItems.reduce((sum, item) => sum + (item.customerAmount || 0), 0)
    };
  }

  removeItem(itemId: string): void {
    this.scannedItems = this.scannedItems.filter(x => x.id !== itemId);
    this.calculateSummary();
  }

  clearAll(): void {
    this.scannedItems = [];
    this.checkoutSummary = null;
    this.barcode = '';
  }

  showPaymentConfirmation(): void {
    if (!this.checkoutSummary || this.checkoutSummary.itemsCount === 0) {
      return;
    }
    this.showPaymentDialog = true;
  }

  hidePaymentDialog(): void {
    this.showPaymentDialog = false;
  }

  checkout(paymentMethod: PaymentMethodType): void {
    if (!this.checkoutSummary || this.checkoutSummary.itemsCount === 0) {
      return;
    }

    const paymentMethodName = paymentMethod === PaymentMethodType.Cash ? 'Gotówka' : 'Karta';

    this.confirmationService.confirm({
      message: `Zapłacić ${this.checkoutSummary.totalAmount.toFixed(2)} PLN za ${this.checkoutSummary.itemsCount} przedmioty metodą ${paymentMethodName}?`,
      header: 'Potwierdź sprzedaż',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.processCheckout(paymentMethod);
      },
    });
  }

  private processCheckout(paymentMethod: PaymentMethodType): void {
    if (!this.checkoutSummary || this.checkoutSummary.itemsCount === 0) {
      return;
    }

    this.isProcessing = true;
    this.hidePaymentDialog();

    this.itemCheckoutService
      .checkoutItems({
        itemSheetItemIds: this.scannedItems.map(x => x.id),
        paymentMethod: paymentMethod,
        totalAmount: this.checkoutSummary.totalAmount,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.isProcessing = false;

          if (result.success) {
            this.messageService.add({
              severity: 'success',
              summary: 'Sukces',
              detail: `Sprzedaż udana! Transakcja: ${result.transactionId}`,
              life: 5000,
            });

            // Reset form
            this.clearAll();
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Płatność odrzucona',
              detail: result.errorMessage || 'Transakcja odrzucona',
            });
          }
        },
        error: error => {
          this.isProcessing = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: error.error?.error?.message || 'Wystąpił błąd podczas sprzedaży',
          });
        },
      });
  }
}