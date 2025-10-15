import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { Observable, Subject } from 'rxjs';
import { takeUntil, finalize } from 'rxjs/operators';
import { LocalizationService } from '@abp/ng.core';
import { PaymentTransactionsService } from '../proxy/controllers/payment-transactions.service';
import { PaymentSuccessViewModel } from '../proxy/payments/models';
import { CommonModule } from '@angular/common';
import { CoreModule } from '@abp/ng.core';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-payment-success',
  templateUrl: './payment-success.component.html',
  styleUrls: ['./payment-success.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    CoreModule,
    ButtonModule,
    ProgressSpinnerModule,
    MessageModule,
    TableModule
  ]
})
export class PaymentSuccessComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  viewModel: PaymentSuccessViewModel | null = null;
  loading = true;
  error = false;
  sessionId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private paymentTransactionService: PaymentTransactionsService,
    private messageService: MessageService,
    private localization: LocalizationService
  ) {}

  ngOnInit(): void {
    this.sessionId = this.route.snapshot.paramMap.get('sessionId');

    if (!this.sessionId) {
      this.showError('PaymentSuccess:InvalidSessionId');
      return;
    }

    this.loadPaymentSuccessData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadPaymentSuccessData(): void {
    this.loading = true;
    this.error = false;

    this.paymentTransactionService.getPaymentSuccessViewModel(this.sessionId!)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.loading = false)
      )
      .subscribe({
        next: (viewModel) => {
          this.viewModel = viewModel;

          if (viewModel.success) {
            this.showSuccessMessage(viewModel.message || '');
          } else {
            this.showWarningMessage(viewModel.message || '');
          }
        },
        error: (error) => {
          console.error('Error loading payment success data:', error);
          this.error = true;
          this.showError('PaymentSuccess:LoadError');
        }
      });
  }

  private showSuccessMessage(message: string): void {
    this.messageService.add({
      severity: 'success',
      summary: this.localization.instant('::Messages:Success'),
      detail: this.localization.instant(message),
      life: 8000
    });
  }

  private showWarningMessage(message: string): void {
    this.messageService.add({
      severity: 'warn',
      summary: this.localization.instant('::Messages:Warning'),
      detail: this.localization.instant(message),
      life: 8000
    });
  }

  private showError(messageKey: string): void {
    this.messageService.add({
      severity: 'error',
      summary: this.localization.instant('::Messages:Error'),
      detail: this.localization.instant(messageKey),
      life: 10000
    });
  }

  navigateToMyRentals(): void {
    this.router.navigate([this.viewModel?.nextStepUrl || '/rentals/my-rentals']);
  }

  navigateToHome(): void {
    this.router.navigate(['/']);
  }

  printReceipt(): void {
    if (!this.viewModel) return;

    const printContent = this.generatePrintContent();
    const printWindow = window.open('', '_blank');

    if (printWindow) {
      printWindow.document.write(printContent);
      printWindow.document.close();
      printWindow.print();
    }
  }

  private generatePrintContent(): string {
    if (!this.viewModel) return '';

    const transaction = this.viewModel.transaction;
    const rentals = this.viewModel.rentals;

    let content = `
      <html>
        <head>
          <title>${this.localization.instant('PaymentSuccess:ReceiptTitle')}</title>
          <style>
            body { font-family: Arial, sans-serif; padding: 20px; }
            .header { text-align: center; margin-bottom: 30px; }
            .section { margin-bottom: 20px; }
            .transaction-details { border: 1px solid #ddd; padding: 15px; margin-bottom: 20px; }
            .rental-item { border-bottom: 1px solid #eee; padding: 10px 0; }
            .rental-item:last-child { border-bottom: none; }
            .total { font-weight: bold; font-size: 18px; text-align: right; margin-top: 20px; }
            .success { color: #28a745; }
            .processing { color: #ffc107; }
          </style>
        </head>
        <body>
          <div class="header">
            <h1>${this.localization.instant('PaymentSuccess:ReceiptTitle')}</h1>
            <p class="${transaction.isCompleted ? 'success' : 'processing'}">
              ${this.localization.instant(transaction.isCompleted ? 'PaymentStatus:Completed' : 'PaymentStatus:Processing')}
            </p>
          </div>

          <div class="transaction-details">
            <h3>${this.localization.instant('PaymentSuccess:TransactionDetails')}</h3>
            <p><strong>${this.localization.instant('PaymentSuccess:TransactionId')}:</strong> ${transaction.sessionId}</p>
            <p><strong>${this.localization.instant('PaymentSuccess:OrderId')}:</strong> ${transaction.orderId || '-'}</p>
            <p><strong>${this.localization.instant('PaymentSuccess:PaymentDate')}:</strong> ${this.viewModel.formattedPaymentDate}</p>
            <p><strong>${this.localization.instant('PaymentSuccess:PaymentMethod')}:</strong> ${this.viewModel.paymentProvider}</p>
            <p><strong>${this.localization.instant('PaymentSuccess:Method')}:</strong> ${transaction.method || '-'}</p>
            <p><strong>${this.localization.instant('PaymentSuccess:Email')}:</strong> ${transaction.email}</p>
          </div>

          <div class="section">
            <h3>${this.localization.instant('PaymentSuccess:RentalsSummary')}</h3>
    `;

    rentals.forEach(rental => {
      const formattedDateRange = `${rental.startDate} - ${rental.endDate}`;
      const formattedAmount = `${rental.totalAmount.toFixed(2)} ${rental.currency}`;

      content += `
        <div class="rental-item">
          <p><strong>${this.localization.instant('::Common:Booth')}:</strong> ${rental.boothNumber}</p>
          <p><strong>${this.localization.instant('::Common:Period')}:</strong> ${formattedDateRange}</p>
          <p><strong>${this.localization.instant('::Common:Days')}:</strong> ${rental.daysCount}</p>
          <p><strong>${this.localization.instant('::Common:Amount')}:</strong> ${formattedAmount}</p>
        </div>
      `;
    });

    content += `
            <div class="total">
              ${this.localization.instant('PaymentSuccess:TotalAmount')}: ${this.viewModel.formattedTotalAmount}
            </div>
          </div>

          <div class="section">
            <p><em>${this.localization.instant('PaymentSuccess:ThankYouMessage')}</em></p>
          </div>
        </body>
      </html>
    `;

    return content;
  }

  getTransactionStatusClass(): string {
    if (!this.viewModel?.transaction) return '';

    return this.viewModel.transaction.isCompleted ? 'success' :
           this.viewModel.transaction.status === 'processing' ? 'processing' : 'warning';
  }

  getStatusIcon(): string {
    if (!this.viewModel?.transaction) return 'pi-question-circle';

    return this.viewModel.transaction.isCompleted ? 'pi-check-circle' :
           this.viewModel.transaction.status === 'processing' ? 'pi-clock' : 'pi-exclamation-triangle';
  }

  retryLoading(): void {
    if (this.sessionId) {
      this.loadPaymentSuccessData();
    }
  }
}