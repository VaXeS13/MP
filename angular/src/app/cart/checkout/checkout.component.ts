import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { PaymentService } from '../../services/payment.service';
import { CartDto, CheckoutCartDto } from '../../shared/models/cart.model';
import { PaymentProvider, PaymentMethod } from '../../shared/models/payment.model';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-checkout',
  standalone: false,
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent implements OnInit {
  cart: CartDto | null = null;
  providers: PaymentProvider[] = [];
  selectedProvider?: PaymentProvider;
  availableMethods: PaymentMethod[] = [];
  selectedMethod?: PaymentMethod;

  loading = false;
  processing = false;
  step: 'review' | 'payment' = 'review';

  constructor(
    private cartService: CartService,
    private paymentService: PaymentService,
    private router: Router,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadCart();
    this.loadPaymentProviders();
  }

  loadCart(): void {
    this.loading = true;
    this.cartService.getMyCart().subscribe({
      next: (cart) => {
        if (!cart || cart.itemCount === 0) {
          this.router.navigate(['/cart']);
          return;
        }
        this.cart = cart;
        this.checkForExpiredDates();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading cart:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load cart'
        });
        this.router.navigate(['/cart']);
      }
    });
  }

  checkForExpiredDates(): void {
    if (!this.cart) return;

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const expiredItems = this.cart.items.filter(item => {
      const startDate = new Date(item.startDate);
      startDate.setHours(0, 0, 0, 0);
      return startDate < today;
    });

    if (expiredItems.length > 0) {
      const itemsText = expiredItems.map(item =>
        `Booth ${item.boothNumber} (start date: ${this.formatDate(item.startDate)})`
      ).join(', ');

      this.messageService.add({
        severity: 'warn',
        summary: 'Expired Items in Cart',
        detail: `The following items have start dates in the past and need to be updated: ${itemsText}. Please go back to cart and update these items.`,
        life: 10000
      });
    }
  }

  hasExpiredItems(): boolean {
    if (!this.cart) return false;

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    return this.cart.items.some(item => {
      const startDate = new Date(item.startDate);
      startDate.setHours(0, 0, 0, 0);
      return startDate < today;
    });
  }

  loadPaymentProviders(): void {
    this.paymentService.getPaymentProviders().subscribe({
      next: (providers) => {
        this.providers = providers.filter(p => p.isActive);
      },
      error: (error) => {
        console.error('Error loading payment providers:', error);
      }
    });
  }

  selectProvider(provider: PaymentProvider): void {
    this.selectedProvider = provider;
    this.selectedMethod = undefined;
    this.loadPaymentMethods(provider.id);
  }

  loadPaymentMethods(providerId: string): void {
    const currency = this.cart?.items[0]?.currency || 'PLN';
    this.paymentService.getPaymentMethods(providerId, currency).subscribe({
      next: (methods) => {
        this.availableMethods = methods.filter(m => m.isActive);
        if (this.availableMethods.length === 1) {
          this.selectedMethod = this.availableMethods[0];
        }
      },
      error: (error) => {
        console.error('Error loading payment methods:', error);
      }
    });
  }

  selectMethod(method: PaymentMethod): void {
    this.selectedMethod = method;
  }

  goToPaymentSelection(): void {
    this.step = 'payment';
  }

  goBackToReview(): void {
    this.step = 'review';
  }

  canProceed(): boolean {
    return !!this.selectedProvider &&
           (this.availableMethods.length === 0 || !!this.selectedMethod) &&
           !this.hasExpiredItems();
  }

  async completeCheckout(): Promise<void> {
    if (!this.canProceed() || !this.cart) {
      if (this.hasExpiredItems()) {
        this.messageService.add({
          severity: 'error',
          summary: 'Cannot Proceed',
          detail: 'Please update items with expired dates before checkout'
        });
      }
      return;
    }

    this.processing = true;

    const checkoutDto: CheckoutCartDto = {
      paymentProviderId: this.selectedProvider!.id,
      paymentMethodId: this.selectedMethod?.id
    };

    this.cartService.checkout(checkoutDto).subscribe({
      next: (result) => {
        if (result.success && result.paymentUrl) {
          // Store checkout info
          localStorage.setItem('checkoutInfo', JSON.stringify({
            rentalIds: result.rentalIds,
            transactionId: result.transactionId,
            timestamp: Date.now()
          }));

          // Redirect to payment
          window.location.href = result.paymentUrl;
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Checkout Failed',
            detail: result.errorMessage || 'Failed to complete checkout'
          });
          this.processing = false;
        }
      },
      error: (error) => {
        console.error('Checkout error:', error);

        // Check if error is about expired dates
        const errorCode = error.error?.error?.code;
        const errorData = error.error?.error?.data;

        if (errorCode === 'RENTAL_START_DATE_IN_PAST') {
          this.messageService.add({
            severity: 'error',
            summary: 'Expired Rental Dates',
            detail: 'One or more items in your cart have start dates in the past. Please update them before checkout.',
            life: 8000
          });
        } else if (errorCode === 'BOOTH_ALREADY_RENTED_IN_PERIOD') {
          this.messageService.add({
            severity: 'error',
            summary: 'Booth No Longer Available',
            detail: 'One or more booths in your cart were just rented by another user. Please remove them and select different dates.',
            life: 8000
          });
        } else if (errorCode === 'RENTAL_CREATES_UNUSABLE_GAP_BEFORE') {
          const gapDays = errorData?.GapDays || 0;
          const minimumGapDays = errorData?.MinimumGapDays || 0;
          const suggestedDate = errorData?.SuggestedStartDate ? new Date(errorData.SuggestedStartDate).toLocaleDateString() : '';
          const alternativeDate = errorData?.AlternativeStartDate ? new Date(errorData.AlternativeStartDate).toLocaleDateString() : '';

          this.messageService.add({
            severity: 'error',
            summary: 'Unusable Gap Before Rental',
            detail: `Your rental would leave a ${gapDays}-day gap before another rental. Minimum gap is ${minimumGapDays} days. Please start on ${suggestedDate} (adjacent) or ${alternativeDate} (with minimum gap).`,
            life: 10000
          });
        } else if (errorCode === 'RENTAL_CREATES_UNUSABLE_GAP_AFTER') {
          const gapDays = errorData?.GapDays || 0;
          const minimumGapDays = errorData?.MinimumGapDays || 0;
          const suggestedDate = errorData?.SuggestedEndDate ? new Date(errorData.SuggestedEndDate).toLocaleDateString() : '';
          const alternativeDate = errorData?.AlternativeEndDate ? new Date(errorData.AlternativeEndDate).toLocaleDateString() : '';

          this.messageService.add({
            severity: 'error',
            summary: 'Unusable Gap After Rental',
            detail: `Your rental would leave a ${gapDays}-day gap after another rental. Minimum gap is ${minimumGapDays} days. Please end on ${suggestedDate} (adjacent) or ${alternativeDate} (with minimum gap).`,
            life: 10000
          });
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Checkout Error',
            detail: error.error?.error?.message || 'An error occurred during checkout'
          });
        }
        this.processing = false;
      }
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString();
  }

  trackByCartItemId(index: number, item: any): string {
    return item.id || index.toString();
  }

  trackByProviderId(index: number, provider: PaymentProvider): string {
    return provider.id;
  }

  trackByMethodId(index: number, method: PaymentMethod): string {
    return method.id;
  }
}
