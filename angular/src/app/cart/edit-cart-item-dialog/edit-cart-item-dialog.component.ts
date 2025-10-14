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
}