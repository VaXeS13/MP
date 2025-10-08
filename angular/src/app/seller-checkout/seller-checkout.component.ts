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

import { SellerService } from '../proxy/http-api/controllers/seller.service';
import {
  ItemForCheckoutDto,
  AvailablePaymentMethodsDto,
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
  item: ItemForCheckoutDto | null = null;
  availablePaymentMethods: AvailablePaymentMethodsDto | null = null;
  isLoading = false;
  isProcessing = false;

  PaymentMethodType = PaymentMethodType;

  constructor(
    private sellerService: SellerService,
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
    this.sellerService
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
    this.item = null;

    this.sellerService
      .findItemByBarcode({ barcode: this.barcode.trim() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.isLoading = false;

          if (result) {
            this.item = result;

            if (result.status !== 'ForSale') {
              this.messageService.add({
                severity: 'warn',
                summary: 'Item Not Available',
                detail: `This item is ${result.status} and cannot be sold`,
              });
            }
          } else {
            this.messageService.add({
              severity: 'info',
              summary: 'Not Found',
              detail: 'No item found with this barcode',
            });
            this.barcode = '';
          }
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

  checkout(paymentMethod: PaymentMethodType): void {
    if (!this.item || !this.item.actualPrice) {
      return;
    }

    const paymentMethodName = paymentMethod === PaymentMethodType.Cash ? 'Cash' : 'Card';

    this.confirmationService.confirm({
      message: `Checkout ${this.item.name} for ${this.item.actualPrice} using ${paymentMethodName}?`,
      header: 'Confirm Checkout',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.processCheckout(paymentMethod);
      },
    });
  }

  private processCheckout(paymentMethod: PaymentMethodType): void {
    if (!this.item || !this.item.actualPrice) {
      return;
    }

    this.isProcessing = true;

    this.sellerService
      .checkoutItem({
        itemSheetItemId: this.item.id,
        paymentMethod: paymentMethod,
        amount: this.item.actualPrice,
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.isProcessing = false;

          if (result.success) {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Item sold successfully! Transaction: ${result.transactionId}`,
              life: 5000,
            });

            // Reset form
            this.item = null;
            this.barcode = '';
          } else {
            this.messageService.add({
              severity: 'error',
              summary: 'Payment Failed',
              detail: result.errorMessage || 'Payment was declined',
            });
          }
        },
        error: error => {
          this.isProcessing = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.error?.message || 'Checkout failed',
          });
        },
      });
  }

  clearItem(): void {
    this.item = null;
    this.barcode = '';
  }
}