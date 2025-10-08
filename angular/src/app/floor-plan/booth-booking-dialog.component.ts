import { Component, OnInit } from '@angular/core';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { MessageService } from 'primeng/api';
import { RentalService } from '../services/rental.service';
import { BoothDto } from '../shared/models/booth.model';
import { FloorPlanDto } from '../shared/models/floor-plan.model';
import { CreateRentalDto } from '../shared/models/rental.model';
import { addDays, format, isAfter, isBefore, isEqual, startOfDay } from 'date-fns';

@Component({
  selector: 'app-booth-booking-dialog',
  standalone: false,
  template: `
    <div class="booth-booking-dialog p-4">
      <!-- Booth Information -->
      <div class="booth-info mb-4 p-3 surface-100 border-round">
        <div class="flex justify-content-between align-items-center mb-3">
          <h4 class="m-0">Stanowisko {{ booth.number }}</h4>
          <p-badge
            [value]="booth.statusDisplayName"
            [severity]="getBoothStatusSeverity(booth.status)">
          </p-badge>
        </div>

        <div class="grid">
          <div class="col-6">
            <div class="flex align-items-center gap-2 mb-2">
              <i class="pi pi-money-bill text-primary"></i>
              <span><strong>{{ booth.pricePerDay }} {{ booth.currencyDisplayName }}</strong> / dzień</span>
            </div>
          </div>
          <div class="col-6">
            <div class="flex align-items-center gap-2 mb-2">
              <i class="pi pi-map text-primary"></i>
              <span>{{ floorPlan?.name }} - Poziom {{ floorPlan?.level }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Booking Form -->
      <form (ngSubmit)="onSubmit()" #bookingForm="ngForm">
        <!-- Date Range Selection -->
        <div class="field mb-4">
          <label for="dateRange" class="block mb-2 font-medium">
            <i class="pi pi-calendar mr-2"></i>
            Okres wynajmu *
          </label>
          <p-calendar
            id="dateRange"
            [(ngModel)]="selectedDateRange"
            name="dateRange"
            selectionMode="range"
            [inline]="true"
            [minDate]="minDate"
            [maxDate]="maxDate"
            [disabledDates]="disabledDates"
            [showButtonBar]="true"
            [readonlyInput]="true"
            dateFormat="dd.mm.yy"
            (onSelect)="onDateRangeChange()"
            required>
          </p-calendar>

          <div *ngIf="selectedDateRange && selectedDateRange[0] && selectedDateRange[1]"
               class="mt-2 text-sm text-color-secondary">
            <i class="pi pi-info-circle mr-1"></i>
            Wybrano {{ getRentalDays() }} dni ({{ formatDate(selectedDateRange[0]) }} - {{ formatDate(selectedDateRange[1]) }})
          </div>
        </div>

        <!-- Price Calculation -->
        <div *ngIf="totalAmount > 0" class="price-summary mb-4 p-3 border surface-border border-round">
          <div class="flex justify-content-between align-items-center mb-2">
            <span>Cena za dzień:</span>
            <span>{{ booth.pricePerDay }} {{ booth.currencyDisplayName }}</span>
          </div>
          <div class="flex justify-content-between align-items-center mb-2">
            <span>Liczba dni:</span>
            <span>{{ getRentalDays() }}</span>
          </div>
          <p-divider></p-divider>
          <div class="flex justify-content-between align-items-center">
            <span class="font-bold">Łączna kwota:</span>
            <span class="font-bold text-primary text-xl">{{ totalAmount }} {{ booth.currencyDisplayName }}</span>
          </div>
        </div>

        <!-- Additional Notes -->
        <div class="field mb-4">
          <label for="notes" class="block mb-2 font-medium">
            <i class="pi pi-comment mr-2"></i>
            Dodatkowe uwagi
          </label>
          <textarea
            id="notes"
            [(ngModel)]="notes"
            name="notes"
            pTextarea
            rows="3"
            placeholder="Opcjonalne uwagi do rezerwacji..."
            class="w-full">
          </textarea>
        </div>

        <!-- Terms and Conditions -->
        <div class="field mb-4">
          <div class="flex align-items-center">
            <p-checkbox
              [(ngModel)]="acceptTerms"
              name="acceptTerms"
              binary="true"
              inputId="acceptTerms"
              required>
            </p-checkbox>
            <label for="acceptTerms" class="ml-2">
              Akceptuję <a href="#" class="text-primary">regulamin</a> i
              <a href="#" class="text-primary">politykę prywatności</a> *
            </label>
          </div>
        </div>

        <!-- Booking Summary -->
        <div *ngIf="selectedDateRange && selectedDateRange[0] && selectedDateRange[1]"
             class="booking-summary mb-4 p-3 surface-50 border-round">
          <h5 class="mb-3">
            <i class="pi pi-check-circle mr-2 text-green-500"></i>
            Podsumowanie rezerwacji
          </h5>
          <div class="grid">
            <div class="col-12 md:col-6">
              <div class="text-sm text-color-secondary">Stanowisko</div>
              <div class="font-medium">{{ booth.number }}</div>
            </div>
            <div class="col-12 md:col-6">
              <div class="text-sm text-color-secondary">Lokalizacja</div>
              <div class="font-medium">{{ floorPlan?.name }}</div>
            </div>
            <div class="col-12 md:col-6">
              <div class="text-sm text-color-secondary">Data rozpoczęcia</div>
              <div class="font-medium">{{ formatDate(selectedDateRange[0]) }}</div>
            </div>
            <div class="col-12 md:col-6">
              <div class="text-sm text-color-secondary">Data zakończenia</div>
              <div class="font-medium">{{ formatDate(selectedDateRange[1]) }}</div>
            </div>
          </div>
        </div>

        <!-- Action Buttons -->
        <div class="flex justify-content-end gap-2">
          <p-button
            label="Anuluj"
            severity="secondary"
            [text]="true"
            (onClick)="cancel()">
          </p-button>
          <p-button
            label="Zarezerwuj"
            icon="pi pi-check"
            type="submit"
            [disabled]="!canSubmit()"
            [loading]="submitting">
          </p-button>
        </div>
      </form>

      <!-- Validation Messages -->
      <div *ngIf="validationErrors.length > 0" class="validation-errors mt-3">
        <p-message
          *ngFor="let error of validationErrors"
          severity="error"
          [text]="error">
        </p-message>
      </div>
    </div>
  `,
  styles: [`
    .booth-booking-dialog {
      max-width: 100%;
    }

    .price-summary {
      background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
    }

    .booking-summary {
      border-left: 4px solid var(--green-500);
    }

    .validation-errors {
      max-height: 200px;
      overflow-y: auto;
    }

    :host ::ng-deep .p-calendar .p-datepicker {
      min-width: 100%;
    }

    :host ::ng-deep .p-calendar .p-datepicker table {
      width: 100%;
    }

    :host ::ng-deep .p-calendar .p-disabled {
      background-color: #ffebee !important;
      color: #c62828 !important;
    }
  `]
})
export class BoothBookingDialogComponent implements OnInit {
  booth!: BoothDto;
  floorPlan?: FloorPlanDto;

  selectedDateRange: Date[] = [];
  notes = '';
  acceptTerms = false;
  totalAmount = 0;
  submitting = false;

  minDate = new Date();
  maxDate = addDays(new Date(), 365); // 1 year ahead
  disabledDates: Date[] = [];

  validationErrors: string[] = [];

  constructor(
    private ref: DynamicDialogRef,
    private config: DynamicDialogConfig,
    private rentalService: RentalService,
    private messageService: MessageService
  ) {
    this.booth = this.config.data.booth;
    this.floorPlan = this.config.data.floorPlan;
  }

  ngOnInit() {
    this.loadUnavailableDates();
    this.setupDateConstraints();
  }

  private setupDateConstraints() {
    // Set minimum date to today
    this.minDate = startOfDay(new Date());

    // Set maximum date to 1 year from now
    this.maxDate = addDays(new Date(), 365);
  }

  private loadUnavailableDates() {
    // In a real application, you would load existing bookings for this booth
    // and mark those dates as disabled

    // Example: Mock some disabled dates
    this.disabledDates = [
      addDays(new Date(), 5),
      addDays(new Date(), 6),
      addDays(new Date(), 12),
      addDays(new Date(), 13),
      addDays(new Date(), 14)
    ];
  }

  onDateRangeChange() {
    this.calculateTotalAmount();
    this.validateDateRange();
  }

  private calculateTotalAmount() {
    if (this.selectedDateRange && this.selectedDateRange[0] && this.selectedDateRange[1]) {
      const days = this.getRentalDays();
      this.totalAmount = days * this.booth.pricePerDay;
    } else {
      this.totalAmount = 0;
    }
  }

  getRentalDays(): number {
    if (!this.selectedDateRange || !this.selectedDateRange[0] || !this.selectedDateRange[1]) {
      return 0;
    }

    const startDate = startOfDay(this.selectedDateRange[0]);
    const endDate = startOfDay(this.selectedDateRange[1]);

    const diffTime = endDate.getTime() - startDate.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    return Math.max(1, diffDays + 1); // Include both start and end date
  }

  private validateDateRange() {
    this.validationErrors = [];

    if (!this.selectedDateRange || !this.selectedDateRange[0] || !this.selectedDateRange[1]) {
      return;
    }

    const startDate = startOfDay(this.selectedDateRange[0]);
    const endDate = startOfDay(this.selectedDateRange[1]);
    const today = startOfDay(new Date());

    // Check if start date is in the past
    if (isBefore(startDate, today)) {
      this.validationErrors.push('Data rozpoczęcia nie może być z przeszłości');
    }

    // Check if end date is before start date
    if (isBefore(endDate, startDate)) {
      this.validationErrors.push('Data zakończenia musi być po dacie rozpoczęcia');
    }

    // Check if dates overlap with disabled dates
    const hasConflict = this.disabledDates.some(disabledDate => {
      const disabled = startOfDay(disabledDate);
      return (isEqual(disabled, startDate) || isEqual(disabled, endDate) ||
              (isAfter(disabled, startDate) && isBefore(disabled, endDate)));
    });

    if (hasConflict) {
      this.validationErrors.push('Wybrane daty pokrywają się z istniejącymi rezerwacjami');
    }

    // Check minimum rental period (e.g., at least 1 day)
    if (this.getRentalDays() < 1) {
      this.validationErrors.push('Minimalny okres wynajmu to 1 dzień');
    }

    // Check maximum rental period (e.g., max 30 days)
    if (this.getRentalDays() > 30) {
      this.validationErrors.push('Maksymalny okres wynajmu to 30 dni');
    }
  }

  formatDate(date: Date): string {
    return format(date, 'dd.MM.yyyy');
  }

  getBoothStatusSeverity(status: number): string {
    switch (status) {
      case 1: return 'success';
      case 2: return 'warning';
      case 3: return 'danger';
      case 4: return 'secondary';
      default: return 'info';
    }
  }

  canSubmit(): boolean {
    return (
      this.selectedDateRange &&
      this.selectedDateRange[0] &&
      this.selectedDateRange[1] &&
      this.acceptTerms &&
      this.validationErrors.length === 0 &&
      this.totalAmount > 0 &&
      !this.submitting
    );
  }

  onSubmit() {
    if (!this.canSubmit()) {
      return;
    }

    this.submitting = true;

    const rentalData: CreateRentalDto = {
      boothId: this.booth.id,
      startDate: this.selectedDateRange[0],
      endDate: this.selectedDateRange[1],
      notes: this.notes.trim() || undefined
    };

    this.rentalService.create(rentalData).subscribe({
      next: (rental) => {
        this.submitting = false;
        this.ref.close({ success: true, rental });
      },
      error: (error) => {
        this.submitting = false;

        let errorMessage = 'Nie udało się utworzyć rezerwacji';

        if (error.error && error.error.message) {
          errorMessage = error.error.message;
        } else if (error.message) {
          errorMessage = error.message;
        }

        this.messageService.add({
          severity: 'error',
          summary: 'Błąd rezerwacji',
          detail: errorMessage
        });
      }
    });
  }

  cancel() {
    this.ref.close({ success: false });
  }
}