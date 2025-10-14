import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { RentalDto } from '../../proxy/rentals/models';
import { RentalPaymentMethod } from '../../proxy/domain/rentals/rental-payment-method.enum';
import { RentalService as ProxyRentalService } from '../../proxy/rentals/rental.service';
import { IdentityUserService } from '@abp/ng.identity/proxy';
import { BoothTypeService } from '../../services/booth-type.service';
import { BoothService } from '../../services/booth.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-admin-booth-rental-dialog',
  templateUrl: './admin-booth-rental-dialog.component.html',
  styleUrls: ['./admin-booth-rental-dialog.component.scss'],
  standalone: false
})
export class AdminBoothRentalDialogComponent implements OnInit, OnChanges {
  @Input() visible = false;
  @Input() boothId: string | null = null;
  @Input() mode: 'new' | 'extend' = 'new'; // Mode: new rental or extension
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() rentalCreatedOrExtended = new EventEmitter<RentalDto>();

  rentalForm!: FormGroup;
  loading = false;
  loadingData = false;
  calculating = false;
  validatingDates = false;

  // For extension mode
  activeRental: RentalDto | null = null;
  maxExtensionDate: Date | null = null;
  boothTypeIdForExtension: string | null = null;

  // Calendar integration
  calendarSelectedDates: {startDate: Date, endDate: Date, isValid: boolean} | null = null;

  // Data for dropdowns
  availableUsers: any[] = [];
  availableBoothTypes: any[] = [];

  // Booth information
  boothCurrency: string = 'PLN';
  boothCurrencySymbol: string = 'zł';

  // Cost calculation
  totalCost = 0;
  daysCount = 0;
  dateValidationError: string | null = null;

  // Payment method options
  paymentMethods = [
    {
      label: 'Free (Gratis)',
      value: RentalPaymentMethod.Free,
      icon: 'pi pi-gift',
      description: 'Free rental - immediately active'
    },
    {
      label: 'Cash (Gotówka)',
      value: RentalPaymentMethod.Cash,
      icon: 'pi pi-money-bill',
      description: 'Cash payment on-site - immediately active'
    },
    {
      label: 'Card Terminal',
      value: RentalPaymentMethod.Terminal,
      icon: 'pi pi-credit-card',
      description: 'Terminal payment with transaction ID'
    },
    {
      label: 'Online Payment',
      value: RentalPaymentMethod.Online,
      icon: 'pi pi-shopping-cart',
      description: 'Add to cart - user pays online'
    }
  ];

  RentalPaymentMethod = RentalPaymentMethod;
  minDate: Date = new Date();

  constructor(
    private fb: FormBuilder,
    private rentalService: ProxyRentalService,
    private identityUserService: IdentityUserService,
    private boothTypeService: BoothTypeService,
    private boothService: BoothService,
    private tenantCurrencyService: TenantCurrencyService,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadTenantCurrency();
  }

  loadTenantCurrency(): void {
    this.tenantCurrencyService.getCurrency().subscribe(result => {
      this.boothCurrency = this.tenantCurrencyService.getCurrencyName(result.currency);
      // Map currency to symbol
      const currencyMap: { [key: string]: string } = {
        'PLN': 'zł',
        'EUR': '€',
        'USD': '$',
        'GBP': '£',
        'CZK': 'Kč'
      };
      this.boothCurrencySymbol = currencyMap[this.boothCurrency] || this.boothCurrency;
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.onShow();
    }
  }

  initForm(): void {
    this.rentalForm = this.fb.group({
      userId: [null], // Required for new rental
      boothTypeId: [null], // Required for new rental
      startDate: [null, Validators.required],
      endDate: [null, Validators.required],
      paymentMethod: [RentalPaymentMethod.Cash, Validators.required],
      terminalTransactionId: [''],
      terminalReceiptNumber: [''],
      notes: [''],
      onlineTimeoutMinutes: [30]
    });

    // Watch for date changes
    this.rentalForm.get('startDate')?.valueChanges.subscribe(() => this.onDatesChange());
    this.rentalForm.get('endDate')?.valueChanges.subscribe(() => this.onDatesChange());

    // Watch for payment method changes
    this.rentalForm.get('paymentMethod')?.valueChanges.subscribe(method => {
      this.updateValidatorsForPaymentMethod(method);
    });
  }

  onShow(): void {
    this.dateValidationError = null;
    this.totalCost = 0;
    this.daysCount = 0;

    // Currency is already loaded in ngOnInit from tenant settings

    if (this.mode === 'extend') {
      this.loadActiveRentalForExtension();
    } else {
      this.loadDataForNewRental();
    }
  }

  loadActiveRentalForExtension(): void {
    if (!this.boothId) return;

    this.loadingData = true;

    this.rentalService.getActiveRentalForBooth(this.boothId).subscribe({
      next: (rental) => {
        if (rental) {
          this.activeRental = rental;
          this.minDate = new Date(rental.endDate);
          this.minDate.setDate(this.minDate.getDate() + 1);

          // Load max extension date and wait for it
          this.rentalService.getMaxExtensionDate(this.boothId!, rental.endDate!).subscribe({
            next: (response) => {
              if (response.hasBlockingRental && response.maxExtensionDate) {
                this.maxExtensionDate = new Date(response.maxExtensionDate);
              } else {
                this.maxExtensionDate = null;
              }
              // Now we can set loading to false
              this.loadingData = false;
              this.cdr.detectChanges();
            },
            error: () => {
              this.maxExtensionDate = null;
              this.loadingData = false;
              this.cdr.detectChanges();
            }
          });

          // Setup form for extension
          this.rentalForm.patchValue({
            startDate: new Date(rental.startDate),
            endDate: null,
            paymentMethod: RentalPaymentMethod.Cash
          });

          // Disable startDate for extension
          this.rentalForm.get('startDate')?.disable();

        } else {
          this.messageService.add({
            severity: 'warn',
            summary: 'No Active Rental',
            detail: 'This booth has no active rental to extend'
          });
          this.loadingData = false;
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load rental information'
        });
        this.loadingData = false;
        this.cdr.detectChanges();
      }
    });
  }

  loadDataForNewRental(): void {
    this.loadingData = true;
    this.minDate = new Date();

    // Load users and booth types in parallel
    forkJoin({
      users: this.identityUserService.getList({
        maxResultCount: 100,
        skipCount: 0
      }),
      boothTypes: this.boothTypeService.getList({
        maxResultCount: 100,
        skipCount: 0
      })
    }).subscribe({
      next: (result) => {
        this.availableUsers = result.users.items.map(user => ({
          id: user.id,
          displayName: `${user.name} ${user.surname} (${user.email})`
        }));

        this.availableBoothTypes = result.boothTypes.items
          .filter(type => type.isActive)
          .map(type => ({
            id: type.id,
            name: type.name
          }));

        this.loadingData = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load data'
        });
        this.loadingData = false;
        this.cdr.detectChanges();
      }
    });

    // Setup form for new rental
    this.rentalForm.get('userId')?.setValidators(Validators.required);
    this.rentalForm.get('boothTypeId')?.setValidators(Validators.required);
    this.rentalForm.get('startDate')?.enable();
    this.rentalForm.updateValueAndValidity();
  }

  updateValidatorsForPaymentMethod(method: RentalPaymentMethod): void {
    const transactionIdControl = this.rentalForm.get('terminalTransactionId');
    const receiptNumberControl = this.rentalForm.get('terminalReceiptNumber');
    const timeoutControl = this.rentalForm.get('onlineTimeoutMinutes');

    // Clear validators
    transactionIdControl?.clearValidators();
    receiptNumberControl?.clearValidators();
    timeoutControl?.clearValidators();

    // Set validators based on payment method
    if (method === RentalPaymentMethod.Terminal) {
      transactionIdControl?.setValidators([Validators.required]);
    } else if (method === RentalPaymentMethod.Online) {
      timeoutControl?.setValidators([Validators.required, Validators.min(1), Validators.max(120)]);
    }

    // Update validity
    transactionIdControl?.updateValueAndValidity();
    receiptNumberControl?.updateValueAndValidity();
    timeoutControl?.updateValueAndValidity();
  }

  async onDatesChange(): Promise<void> {
    const startDate = this.rentalForm.get('startDate')?.value;
    const endDate = this.rentalForm.get('endDate')?.value;

    if (!startDate || !endDate) {
      this.totalCost = 0;
      this.daysCount = 0;
      return;
    }

    // Validate dates
    if (endDate <= startDate) {
      this.dateValidationError = 'End date must be after start date';
      this.totalCost = 0;
      this.daysCount = 0;
      return;
    }

    // Calculate days
    const start = new Date(startDate);
    const end = new Date(endDate);
    this.daysCount = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1;

    // Validate minimum 7 days
    if (this.daysCount < 7) {
      this.dateValidationError = 'Rental period must be at least 7 days';
      this.totalCost = 0;
      return;
    }

    this.dateValidationError = null;

    // Calculate cost
    await this.calculateCost();
  }

  async calculateCost(): Promise<void> {
    const startDate = this.rentalForm.get('startDate')?.value;
    const endDate = this.rentalForm.get('endDate')?.value;
    const boothTypeId = this.rentalForm.get('boothTypeId')?.value;

    if (!this.boothId || !startDate || !endDate) return;

    // For extension, use boothTypeIdForExtension; for new rental, use form value
    const typeId = this.mode === 'extend'
      ? this.boothTypeIdForExtension
      : boothTypeId;

    if (!typeId) return;

    this.calculating = true;

    try {
      const cost = await this.rentalService.calculateCost(
        this.boothId,
        typeId,
        this.formatDate(startDate),
        this.formatDate(endDate)
      ).toPromise();

      this.totalCost = cost || 0;
    } catch (error) {
      console.error('Error calculating cost:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Failed to calculate cost'
      });
      this.totalCost = 0;
    } finally {
      this.calculating = false;
    }
  }

  onSubmit(): void {
    if (this.rentalForm.invalid || this.dateValidationError) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Invalid Form',
        detail: 'Please fill in all required fields correctly'
      });
      return;
    }

    this.loading = true;

    const formValue = this.rentalForm.value;

    const dto = {
      boothId: this.boothId!,
      userId: this.mode === 'new' ? formValue.userId : undefined,
      boothTypeId: this.mode === 'new' ? formValue.boothTypeId : undefined,
      startDate: this.formatDate(formValue.startDate),
      endDate: this.formatDate(formValue.endDate),
      paymentMethod: formValue.paymentMethod,
      terminalTransactionId: formValue.terminalTransactionId || undefined,
      terminalReceiptNumber: formValue.terminalReceiptNumber || undefined,
      notes: formValue.notes || undefined,
      isExtension: this.mode === 'extend',
      existingRentalId: this.mode === 'extend' && this.activeRental ? this.activeRental.id : undefined,
      onlineTimeoutMinutes: formValue.onlineTimeoutMinutes || 30
    };

    this.rentalService.adminManageBoothRental(dto).subscribe({
      next: (rental) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: this.getSuccessMessage()
        });
        this.rentalCreatedOrExtended.emit(rental);
        this.hide();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error managing rental:', error);
        const errorMessage = error.error?.error?.message || 'Failed to manage rental';
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: errorMessage
        });
        this.loading = false;
      }
    });
  }

  getSuccessMessage(): string {
    const paymentMethod = this.rentalForm.get('paymentMethod')?.value;

    if (this.mode === 'extend') {
      switch (paymentMethod) {
        case RentalPaymentMethod.Free:
          return 'Rental extended successfully (Free)';
        case RentalPaymentMethod.Cash:
          return 'Rental extended successfully. Cash payment recorded.';
        case RentalPaymentMethod.Terminal:
          return 'Rental extended successfully. Terminal payment recorded.';
        case RentalPaymentMethod.Online:
          return 'Extension added to cart. User needs to complete payment.';
        default:
          return 'Rental extended successfully';
      }
    } else {
      switch (paymentMethod) {
        case RentalPaymentMethod.Free:
          return 'Rental created successfully (Free) - Immediately active';
        case RentalPaymentMethod.Cash:
          return 'Rental created successfully. Cash payment recorded - Immediately active';
        case RentalPaymentMethod.Terminal:
          return 'Rental created successfully. Terminal payment recorded - Immediately active';
        case RentalPaymentMethod.Online:
          return 'Rental added to cart. User needs to complete payment.';
        default:
          return 'Rental created successfully';
      }
    }
  }

  hide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.rentalForm.reset();
    this.activeRental = null;
    this.totalCost = 0;
    this.daysCount = 0;
    this.dateValidationError = null;
  }

  formatDate(date: Date): string {
    if (!date) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  get dialogTitle(): string {
    return this.mode === 'extend' ? 'Extend Booth Rental' : 'Create New Booth Rental';
  }

  get selectedPaymentMethod(): RentalPaymentMethod {
    return this.rentalForm.get('paymentMethod')?.value;
  }

  get isFormValid(): boolean {
    return this.rentalForm.valid && !this.dateValidationError && this.totalCost >= 0;
  }

  // Calendar event handlers
  onCalendarDatesSelected(event: {startDate: Date, endDate: Date, isValid: boolean}): void {
    console.log('Admin Dialog: Calendar dates selected:', event);
    this.calendarSelectedDates = event;

    // Update form with selected dates
    this.rentalForm.patchValue({
      startDate: event.startDate,
      endDate: event.endDate
    }, { emitEvent: false }); // Don't emit to avoid triggering onDatesChange

    // Manually trigger validation and calculation
    this.onDatesChange();
  }

  onCalendarValidationError(error: string | null): void {
    console.log('Admin Dialog: Calendar validation error:', error);
    this.dateValidationError = error;
  }

  onCalendarCostCalculated(event: {cost: number, days: number}): void {
    console.log('Admin Dialog: Calendar cost calculated:', event);
    this.totalCost = event.cost;
    this.daysCount = event.days;
  }
}
