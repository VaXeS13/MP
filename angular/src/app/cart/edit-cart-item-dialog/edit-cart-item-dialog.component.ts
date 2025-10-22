import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BoothService } from '../../services/booth.service';
import { BoothTypeService } from '../../services/booth-type.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { CartItemDto } from '../../shared/models/cart.model';
import { BoothDto } from '../../shared/models/booth.model';
import { BoothTypeDto } from '../../shared/models/booth-type.model';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';

@Component({
  selector: 'app-edit-cart-item-dialog',
  templateUrl: './edit-cart-item-dialog.component.html',
  styleUrls: ['./edit-cart-item-dialog.component.scss'],
  standalone: false
})
export class EditCartItemDialogComponent implements OnInit, OnChanges {
  @Input() visible = false;
  @Input() cartItem?: CartItemDto;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() saved = new EventEmitter<{
    boothTypeId: string;
    startDate: string;
    endDate: string;
    notes?: string;
  }>();

  booth?: BoothDto;
  boothTypes: BoothTypeDto[] = [];
  selectedBoothType?: BoothTypeDto;

  notes: string = '';
  calculatedPrice = 0;
  calculatedDays = 0;
  loading = false;
  hasGapError = false;
  dateValidationError: string | null = null;
  tenantCurrencyCode: string = 'PLN';

  // For calendar component
  calendarSelectedDates?: {startDate: Date, endDate: Date, isValid: boolean};

  constructor(
    private boothService: BoothService,
    private boothTypeService: BoothTypeService,
    private tenantCurrencyService: TenantCurrencyService,
    private messageService: MessageService,
    private localization: LocalizationService
  ) {}

  ngOnInit(): void {
    // Load tenant currency
    this.tenantCurrencyService.getCurrency().subscribe(result => {
      this.tenantCurrencyCode = this.tenantCurrencyService.getCurrencyName(result.currency);
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    // When dialog becomes visible and cartItem is set, load data
    if (changes['visible'] && this.visible && this.cartItem) {
      this.loadData();
    }
  }

  /**
   * Converts a Date object to YYYY-MM-DD format using local timezone
   * This prevents timezone conversion issues when sending dates to the server
   */
  private formatDateToLocal(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  loadData(): void {
    if (!this.cartItem) return;

    this.loading = true;

    // Load booth
    this.boothService.get(this.cartItem.boothId).subscribe({
      next: (booth) => {
        this.booth = booth;
        this.loadBoothTypes();
      },
      error: (error) => {
        console.error('Error loading booth:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('MP::Error', 'Error'),
          detail: this.localization.instant('MP::FailedToLoadBoothData', 'Failed to load booth data')
        });
        this.loading = false;
      }
    });
  }

  loadBoothTypes(): void {
    if (!this.booth) return;

    this.boothTypeService.getActiveTypes().subscribe({
      next: (types) => {
        this.boothTypes = types;
        this.selectedBoothType = types.find(t => t.id === this.cartItem?.boothTypeId);
        this.notes = this.cartItem?.notes || '';
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading booth types:', error);
        this.loading = false;
      }
    });
  }

  // Calendar event handlers
  onCalendarDatesSelected(event: {startDate: Date, endDate: Date, isValid: boolean}): void {
    this.calendarSelectedDates = event;
    this.hasGapError = !event.isValid;
  }

  onCalendarValidationError(error: string | null): void {
    this.dateValidationError = error;
  }

  onCalendarCostCalculated(event: {cost: number, days: number}): void {
    this.calculatedPrice = event.cost;
    this.calculatedDays = event.days;
  }

  onBoothTypeChange(): void {
    // Booth type change will trigger recalculation in rental-calendar component
  }

  canSave(): boolean {
    return !!(this.calendarSelectedDates &&
              this.calendarSelectedDates.startDate &&
              this.calendarSelectedDates.endDate &&
              this.selectedBoothType &&
              this.calculatedDays >= 7 &&
              !this.hasGapError);
  }

  save(): void {
    if (!this.canSave() || !this.calendarSelectedDates || !this.selectedBoothType) {
      return;
    }

    this.saved.emit({
      boothTypeId: this.selectedBoothType.id,
      startDate: this.formatDateToLocal(this.calendarSelectedDates.startDate),
      endDate: this.formatDateToLocal(this.calendarSelectedDates.endDate),
      notes: this.notes
    });

    this.close();
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  trackByBoothTypeId(index: number, boothType: BoothTypeDto): string {
    return boothType.id;
  }

  /**
   * Get pricing periods display for booth
   * Shows all available pricing periods (e.g., "1 dzień: 1,00 zł, 3 dni: 2,00 zł, 7 dni: 2,00 zł")
   */
  getPricingPeriodsDisplay(): string {
    if (!this.booth || !this.booth.pricingPeriods || this.booth.pricingPeriods.length === 0) {
      return this.formatCurrency(this.booth?.pricePerDay || 0) + ' per day';
    }

    const periods = this.booth.pricingPeriods
      .sort((a, b) => a.days - b.days)
      .map(period => {
        const dayLabel = period.days === 1 ? 'dzień' : 'dni';
        return `${period.days} ${dayLabel}: ${this.formatCurrency(period.pricePerPeriod)}`;
      })
      .join(', ');

    return periods;
  }

  /**
   * Get price breakdown text for current selection
   * Shows detailed breakdown when using multi-period pricing
   */
  getPriceBreakdownText(): string {
    if (!this.booth || !this.booth.pricingPeriods || this.booth.pricingPeriods.length === 0 || !this.calculatedDays) {
      // Fallback to simple format
      return `${this.calculatedDays} days × ${this.formatCurrency(this.booth?.pricePerDay || 0)}`;
    }

    const sortedPeriods = [...this.booth.pricingPeriods].sort((a, b) => b.days - a.days);
    let remainingDays = this.calculatedDays;
    const breakdown: string[] = [];

    // Greedy algorithm breakdown
    for (const period of sortedPeriods) {
      const count = Math.floor(remainingDays / period.days);
      if (count > 0) {
        const dayLabel = period.days === 1 ? 'dzień' : 'dni';
        const subtotal = count * period.pricePerPeriod;
        // Only show count if more than 1
        const countLabel = count === 1 ? '' : `${count}× `;
        breakdown.push(`${countLabel}${period.days} ${dayLabel} (${this.formatCurrency(subtotal)})`);
        remainingDays -= count * period.days;
      }

      if (remainingDays === 0) {
        break;
      }
    }

    // Remaining days
    if (remainingDays > 0) {
      const smallestPeriod = sortedPeriods[sortedPeriods.length - 1];
      const pricePerDay = smallestPeriod.pricePerPeriod / smallestPeriod.days;
      const subtotal = remainingDays * pricePerDay;
      const dayLabel = remainingDays === 1 ? 'dzień' : 'dni';
      breakdown.push(`${remainingDays} ${dayLabel} (${this.formatCurrency(subtotal)})`);
    }

    return breakdown.join('<br>');
  }

  /**
   * Format currency value for display
   */
  private formatCurrency(value: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: this.tenantCurrencyCode || 'PLN'
    }).format(value);
  }
}