import { Component, OnInit, OnChanges, SimpleChanges, Input, Output, EventEmitter } from '@angular/core';
import { PaymentProvider, PaymentMethod, PaymentMethodType } from '../../shared/models/payment.model';
import { PaymentService } from '../../services/payment.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-payment-selection',
  standalone: false,
  templateUrl: './payment-selection.component.html',
  styleUrl: './payment-selection.component.scss'
})
export class PaymentSelectionComponent implements OnInit, OnChanges {
  @Input() amount: number = 0;
  @Input() currency: string = 'PLN';
  @Input() visible: boolean = false;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() paymentSelected = new EventEmitter<{provider: PaymentProvider, method?: PaymentMethod}>();
  @Output() cancelled = new EventEmitter<void>();

  providers: PaymentProvider[] = [];
  selectedProvider?: PaymentProvider;
  availableMethods: PaymentMethod[] = [];
  selectedMethod?: PaymentMethod;
  loadingProviders = false;
  loadingMethods = false;
  step: 'provider' | 'method' = 'provider';

  constructor(
    private paymentService: PaymentService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    if (this.visible) {
      this.loadPaymentProviders();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible']) {
      console.log('PaymentSelectionComponent: visible changed from', changes['visible'].previousValue, 'to', changes['visible'].currentValue);
      console.log('PaymentSelectionComponent: Dialog state:', {
        visible: this.visible,
        amount: this.amount,
        currency: this.currency,
        step: this.step,
        selectedProvider: this.selectedProvider
      });

      // CRITICAL: Check how dialog was opened
      console.log('PaymentSelectionComponent: Stack trace of dialog opening:');
      console.trace();

      if (changes['visible'].currentValue && !changes['visible'].previousValue) {
        // Dialog został otwarty
        console.log('PaymentSelectionComponent: Dialog opened, loading providers...');
        this.loadPaymentProviders();
        this.resetSelection();
      }
    }
  }

  onVisibleChange(): void {
    if (this.visible) {
      this.loadPaymentProviders();
      this.resetSelection();
    }
  }

  private resetSelection(): void {
    this.selectedProvider = undefined;
    this.selectedMethod = undefined;
    this.availableMethods = [];
    this.step = 'provider';
  }

  private loadPaymentProviders(): void {
    console.log('PaymentSelectionComponent: Loading payment providers...');
    this.loadingProviders = true;

    this.paymentService.getPaymentProviders().subscribe({
      next: (providers) => {
        console.log('PaymentSelectionComponent: Received providers:', providers);
        this.providers = providers.filter(p => p.isActive);
        console.log('PaymentSelectionComponent: Active providers:', this.providers);
        this.loadingProviders = false;
      },
      error: (error) => {
        console.error('Error loading payment providers:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load payment providers'
        });
        this.loadingProviders = false;
      }
    });
  }

  selectProvider(provider: PaymentProvider): void {
    this.selectedProvider = provider;
    this.loadPaymentMethods();
  }

  private loadPaymentMethods(): void {
    if (!this.selectedProvider) return;

    this.loadingMethods = true;
    this.step = 'method';

    this.paymentService.getPaymentMethods(this.selectedProvider.id, this.currency).subscribe({
      next: (methods) => {
        console.log('PaymentSelection: Received payment methods:', methods);
        this.availableMethods = methods.filter(m => m.isActive);
        this.loadingMethods = false;
        console.log('PaymentSelection: Step is now:', this.step);
        console.log('PaymentSelection: Available methods:', this.availableMethods.length);

        // If only one method available, select it automatically
        if (this.availableMethods.length === 1) {
          this.selectedMethod = this.availableMethods[0];
          console.log('PaymentSelection: Auto-selected method:', this.selectedMethod);
        }
      },
      error: (error) => {
        console.error('Error loading payment methods:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load payment methods'
        });
        this.loadingMethods = false;
      }
    });
  }

  selectMethod(method: PaymentMethod): void {
    console.log('PaymentSelection: selectMethod called', method);
    this.selectedMethod = method;
    console.log('PaymentSelection: selectedMethod updated', this.selectedMethod);
  }

  goBack(): void {
    if (this.step === 'method') {
      this.step = 'provider';
      this.selectedMethod = undefined;
      this.availableMethods = [];
    }
  }

  confirm(): void {
    console.log('PaymentSelection: confirm() called', {
      step: this.step,
      selectedProvider: this.selectedProvider,
      selectedMethod: this.selectedMethod,
      canConfirm: this.canConfirm(),
      amount: this.amount,
      currency: this.currency
    });

    if (this.selectedProvider) {
      console.log('PaymentSelection: Emitting paymentSelected event with data:', {
        provider: this.selectedProvider,
        method: this.selectedMethod
      });

      // Sprawdź czy event zostaje prawidłowo wyemitowany
      setTimeout(() => {
        console.log('PaymentSelection: Event should have been emitted by now');
      }, 100);

      this.paymentSelected.emit({
        provider: this.selectedProvider,
        method: this.selectedMethod
      });
    } else {
      console.error('PaymentSelection: No provider selected!');
    }
  }

  cancel(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.cancelled.emit();
    this.resetSelection();
  }

  canConfirm(): boolean {
    const result = !!this.selectedProvider && (
      this.availableMethods.length === 0 || // No methods needed
      !!this.selectedMethod // Method selected
    );
    console.log('PaymentSelection: canConfirm() =', result, {
      hasProvider: !!this.selectedProvider,
      methodsCount: this.availableMethods.length,
      hasMethod: !!this.selectedMethod
    });
    return result;
  }

  getMethodTypeIcon(method: PaymentMethod): string {
    if (method.type === undefined || method.type === null) return 'fas fa-credit-card';

    switch (method.type) {
      case PaymentMethodType.BankTransfer:
        return 'fas fa-university';
      case PaymentMethodType.CreditCard:
      case PaymentMethodType.DebitCard:
        return 'fas fa-credit-card';
      case PaymentMethodType.DigitalWallet:
        return 'fas fa-wallet';
      case PaymentMethodType.Cryptocurrency:
        return 'fab fa-bitcoin';
      case PaymentMethodType.BLIK:
        return 'fas fa-mobile-alt';
      case PaymentMethodType.PayByLink:
        return 'fas fa-link';
      case PaymentMethodType.Other:
        return 'fas fa-credit-card';
      default:
        return 'fas fa-credit-card';
    }
  }

  getPaymentTypeLabel(type: PaymentMethodType): string {
    switch (type) {
      case PaymentMethodType.BankTransfer:
        return 'Bank Transfer';
      case PaymentMethodType.CreditCard:
        return 'Credit Card';
      case PaymentMethodType.DebitCard:
        return 'Debit Card';
      case PaymentMethodType.DigitalWallet:
        return 'Digital Wallet';
      case PaymentMethodType.Cryptocurrency:
        return 'Crypto';
      case PaymentMethodType.BLIK:
        return 'BLIK';
      case PaymentMethodType.PayByLink:
        return 'Pay by Link';
      case PaymentMethodType.Other:
        return 'Payment';
      default:
        return 'Payment';
    }
  }

  trackByProviderId(index: number, provider: PaymentProvider): string {
    return provider.id;
  }

  trackByMethodId(index: number, method: PaymentMethod): string {
    return method.id;
  }
}
