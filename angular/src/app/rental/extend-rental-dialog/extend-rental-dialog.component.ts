import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { RentalDto, ExtendRentalDto, ExtensionPaymentType } from '../../shared/models/rental.model';
import { RentalService } from '../../services/rental.service';

@Component({
  selector: 'app-extend-rental-dialog',
  templateUrl: './extend-rental-dialog.component.html',
  styleUrls: ['./extend-rental-dialog.component.scss'],
  standalone: false
})
export class ExtendRentalDialogComponent implements OnInit {
  @Input() visible = false;
  @Input() boothId: string | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() rentalExtended = new EventEmitter<RentalDto>();

  rental: RentalDto | null = null;
  extensionForm!: FormGroup;
  loading = false;
  loadingRental = false;
  calculating = false;
  validatingDates = false;
  extensionCost = 0;
  additionalDays = 0;
  minDate: Date = new Date();
  maxDate: Date | null = null;
  noActiveRentalError = false;

  // Payment type options
  paymentTypes = [
    { label: 'Free (Gratis)', value: ExtensionPaymentType.Free, icon: 'pi pi-gift', description: 'Free extension, zero cost' },
    { label: 'Cash (Gotówka)', value: ExtensionPaymentType.Cash, icon: 'pi pi-money-bill', description: 'Cash payment on-site, instant' },
    { label: 'Card Terminal (Karta)', value: ExtensionPaymentType.Terminal, icon: 'pi pi-credit-card', description: 'Card payment via terminal' },
    { label: 'Online Payment', value: ExtensionPaymentType.Online, icon: 'pi pi-shopping-cart', description: 'Add to cart, pay online' }
  ];

  ExtensionPaymentType = ExtensionPaymentType;

  dateValidationError: string | null = null;

  constructor(
    private fb: FormBuilder,
    private rentalService: RentalService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.initForm();
  }

  initForm(): void {
    this.extensionForm = this.fb.group({
      newEndDate: [null, Validators.required],
      paymentType: [ExtensionPaymentType.Cash, Validators.required],
      terminalTransactionId: [''],
      terminalReceiptNumber: [''],
      onlineTimeoutMinutes: [30]
    });

    // Watch for date changes to validate and calculate cost
    this.extensionForm.get('newEndDate')?.valueChanges.subscribe(date => {
      if (date) {
        this.validateNewEndDate(date);
      }
    });

    // Watch for payment type changes to adjust validators
    this.extensionForm.get('paymentType')?.valueChanges.subscribe(type => {
      this.updateValidators(type);
    });
  }

  onShow(): void {
    if (this.boothId) {
      this.loadingRental = true;
      this.noActiveRentalError = false;
      this.rental = null;

      this.rentalService.getActiveRentalForBooth(this.boothId).subscribe({
        next: (rental) => {
          if (rental) {
            this.rental = rental;
            this.minDate = new Date(rental.endDate);
            this.minDate.setDate(this.minDate.getDate() + 1); // Next day after current end date
            this.extensionForm.reset({
              newEndDate: null,
              paymentType: ExtensionPaymentType.Cash,
              terminalTransactionId: '',
              terminalReceiptNumber: '',
              onlineTimeoutMinutes: 30
            });
            this.extensionCost = 0;
            this.additionalDays = 0;
            this.dateValidationError = null;
            this.noActiveRentalError = false;
          } else {
            this.noActiveRentalError = true;
            this.messageService.add({
              severity: 'warn',
              summary: 'No Active Rental',
              detail: 'This booth currently has no active rental to extend'
            });
          }
          this.loadingRental = false;
        },
        error: (error) => {
          console.error('Error loading active rental:', error);
          this.noActiveRentalError = true;
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load rental information'
          });
          this.loadingRental = false;
        }
      });
    }
  }

  onHide(): void {
    this.extensionForm.reset();
    this.extensionCost = 0;
    this.additionalDays = 0;
    this.dateValidationError = null;
  }

  updateValidators(paymentType: ExtensionPaymentType): void {
    const transactionIdControl = this.extensionForm.get('terminalTransactionId');
    const receiptNumberControl = this.extensionForm.get('terminalReceiptNumber');
    const timeoutControl = this.extensionForm.get('onlineTimeoutMinutes');

    // Clear all validators first
    transactionIdControl?.clearValidators();
    receiptNumberControl?.clearValidators();
    timeoutControl?.clearValidators();

    // Add validators based on payment type
    if (paymentType === ExtensionPaymentType.Terminal) {
      transactionIdControl?.setValidators([Validators.required]);
    } else if (paymentType === ExtensionPaymentType.Online) {
      timeoutControl?.setValidators([Validators.required, Validators.min(1), Validators.max(120)]);
    }

    // Update validity
    transactionIdControl?.updateValueAndValidity();
    receiptNumberControl?.updateValueAndValidity();
    timeoutControl?.updateValueAndValidity();
  }

  async validateNewEndDate(newEndDate: Date): Promise<void> {
    if (!this.rental) return;

    this.validatingDates = true;
    this.dateValidationError = null;

    try {
      // Convert date to YYYY-MM-DD format
      const newEndDateStr = this.formatDate(newEndDate);
      const currentEndDateStr = this.rental.endDate;

      // Check if new date is after current end date
      if (newEndDateStr <= currentEndDateStr) {
        this.dateValidationError = 'New end date must be after current end date';
        this.extensionCost = 0;
        this.additionalDays = 0;
        this.validatingDates = false;
        return;
      }

      // Calculate additional days
      const currentEnd = new Date(currentEndDateStr);
      const newEnd = new Date(newEndDateStr);
      this.additionalDays = Math.ceil((newEnd.getTime() - currentEnd.getTime()) / (1000 * 60 * 60 * 24));

      // Check if extension creates overlap with other rentals
      // We check from the day after current end date to the new end date
      const checkStartDate = new Date(currentEnd);
      checkStartDate.setDate(checkStartDate.getDate() + 1);

      const isAvailable = await this.rentalService.checkBoothAvailability(
        this.rental.boothId,
        this.formatDate(checkStartDate),
        newEndDateStr
      ).toPromise();

      if (!isAvailable) {
        this.dateValidationError = 'Selected period overlaps with another rental. Please choose a different end date.';
        this.extensionCost = 0;
        this.validatingDates = false;
        return;
      }

      // Calculate extension cost
      await this.calculateExtensionCost(newEndDateStr);

    } catch (error) {
      console.error('Error validating dates:', error);
      this.dateValidationError = 'Error validating availability. Please try again.';
      this.extensionCost = 0;
    } finally {
      this.validatingDates = false;
    }
  }

  async calculateExtensionCost(newEndDateStr: string): Promise<void> {
    if (!this.rental) return;

    this.calculating = true;

    try {
      // Calculate cost from day after current end date to new end date
      const currentEnd = new Date(this.rental.endDate);
      const checkStartDate = new Date(currentEnd);
      checkStartDate.setDate(checkStartDate.getDate() + 1);

      const cost = await this.rentalService.calculateRentalCost(
        this.rental.boothId,
        this.rental.boothTypeId,
        this.formatDate(checkStartDate),
        newEndDateStr
      ).toPromise();

      this.extensionCost = cost || 0;

    } catch (error) {
      console.error('Error calculating cost:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Failed to calculate extension cost'
      });
      this.extensionCost = 0;
    } finally {
      this.calculating = false;
    }
  }

  onSubmit(): void {
    if (this.extensionForm.invalid || !this.rental || this.dateValidationError) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Invalid Form',
        detail: 'Please fill in all required fields correctly'
      });
      return;
    }

    this.loading = true;

    const formValue = this.extensionForm.value;
    const dto: ExtendRentalDto = {
      rentalId: this.rental.id,
      newEndDate: this.formatDate(formValue.newEndDate),
      paymentType: formValue.paymentType,
      terminalTransactionId: formValue.terminalTransactionId || undefined,
      terminalReceiptNumber: formValue.terminalReceiptNumber || undefined,
      onlineTimeoutMinutes: formValue.onlineTimeoutMinutes || undefined
    };

    this.rentalService.extendRental(this.rental.id, dto).subscribe({
      next: (updatedRental) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: this.getSuccessMessage(formValue.paymentType)
        });
        this.rentalExtended.emit(updatedRental);
        this.hide();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error extending rental:', error);
        const errorMessage = error.error?.error?.message || 'Failed to extend rental';
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: errorMessage
        });
        this.loading = false;
      }
    });
  }

  getSuccessMessage(paymentType: ExtensionPaymentType): string {
    switch (paymentType) {
      case ExtensionPaymentType.Free:
        return 'Rental extended successfully (Free)';
      case ExtensionPaymentType.Cash:
        return 'Rental extended successfully. Cash payment recorded.';
      case ExtensionPaymentType.Terminal:
        return 'Rental extended successfully. Terminal payment recorded.';
      case ExtensionPaymentType.Online:
        return 'Extension added to cart. Please complete payment to finalize.';
      default:
        return 'Rental extended successfully';
    }
  }

  hide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  formatDate(date: Date): string {
    if (!date) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  get selectedPaymentType(): ExtensionPaymentType {
    return this.extensionForm.get('paymentType')?.value;
  }

  get isFormValid(): boolean {
    return this.extensionForm.valid && !this.dateValidationError && this.extensionCost >= 0;
  }
}
