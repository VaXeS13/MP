import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BoothService } from '../../services/booth.service';
import { BoothTypeService } from '../../services/booth-type.service';
import { RentalService } from '../../services/rental.service';
import { BoothSettingsService } from '../../services/booth-settings.service';
import { CartItemDto } from '../../shared/models/cart.model';
import { BoothDto } from '../../shared/models/booth.model';
import { BoothTypeDto } from '../../shared/models/booth-type.model';
import { BoothCalendarRequestDto, CalendarDateDto, CalendarDateStatus } from '../../shared/models/rental.model';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';

export interface CalendarDay {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelectable: boolean;
  isSelected: boolean;
  isRangeStart: boolean;
  isRangeEnd: boolean;
  isInRange: boolean;
  status: CalendarDateStatus;
  statusDisplayName: string;
}

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

  currentDate = new Date();
  calendarDays: CalendarDay[] = [];
  calendarData: CalendarDateDto[] = [];
  minimumGapDays = 7; // Default value, will be loaded from settings

  selectedStartDate?: Date;
  selectedEndDate?: Date;
  isSelectingRange = false;
  notes: string = '';

  calculatedPrice = 0;
  calculatedDays = 0;
  loading = false;
  hasGapError = false;

  CalendarDateStatus = CalendarDateStatus;

  constructor(
    private boothService: BoothService,
    private boothTypeService: BoothTypeService,
    private rentalService: RentalService,
    private messageService: MessageService,
    private boothSettingsService: BoothSettingsService,
    private localization: LocalizationService
  ) {}

  ngOnInit(): void {
    // Component initialized
  }

  ngOnChanges(changes: SimpleChanges): void {
    // When dialog becomes visible and cartItem is set, load data
    if (changes['visible'] && this.visible && this.cartItem) {
      this.loadBoothSettings();
      this.loadData();
    }
  }

  loadBoothSettings(): void {
    this.boothSettingsService.get().subscribe({
      next: (settings) => {
        this.minimumGapDays = settings.minimumGapDays;
      },
      error: (error) => {
        console.error('Error loading booth settings:', error);
        // Use default value (7) if settings can't be loaded
      }
    });
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

        // Set initial dates and notes
        if (this.cartItem) {
          // Parse dates in local timezone to avoid UTC conversion issues
          this.selectedStartDate = this.parseDateInLocalTimezone(this.cartItem.startDate);
          this.selectedEndDate = this.parseDateInLocalTimezone(this.cartItem.endDate);
          this.notes = this.cartItem.notes || '';
        }

        this.loadCalendar();
      },
      error: (error) => {
        console.error('Error loading booth types:', error);
        this.loading = false;
      }
    });
  }

  /**
   * Parses a date string (YYYY-MM-DD or YYYY-MM-DDTHH:mm:ss) in local timezone
   * This prevents timezone conversion issues
   */
  private parseDateInLocalTimezone(dateString: string): Date {
    if (!dateString) {
      return new Date();
    }

    // Extract only the date part (before 'T' if present)
    const datePart = dateString.split('T')[0];
    const parts = datePart.split('-');

    if (parts.length !== 3) {
      console.error('Invalid date string format:', dateString);
      return new Date();
    }

    const year = parseInt(parts[0], 10);
    const month = parseInt(parts[1], 10);
    const day = parseInt(parts[2], 10);

    if (isNaN(year) || isNaN(month) || isNaN(day)) {
      console.error('Invalid date parts:', { year, month, day, originalString: dateString });
      return new Date();
    }

    return new Date(year, month - 1, day);
  }

  loadCalendar(): void {
    if (!this.cartItem) return;

    // Load calendar data including previous and next month to show days from adjacent months
    const startDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1, 1);
    const endDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 2, 0);

    const request: BoothCalendarRequestDto = {
      boothId: this.cartItem.boothId,
      startDate: this.formatDateToLocal(startDate),
      endDate: this.formatDateToLocal(endDate),
      excludeCartId: this.cartItem.cartId // Exclude current cart to allow same dates
    };

    this.rentalService.getBoothCalendar(request).subscribe({
      next: (response) => {
        this.calendarData = response.dates;
        this.generateCalendar();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading calendar:', error);
        this.loading = false;
      }
    });
  }

  generateCalendar(): void {
    const year = this.currentDate.getFullYear();
    const month = this.currentDate.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const startingDayOfWeek = firstDay.getDay();
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    this.calendarDays = [];

    // Previous month days
    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = startingDayOfWeek - 1; i >= 0; i--) {
      const date = new Date(year, month - 1, prevMonthLastDay - i);
      this.calendarDays.push(this.createCalendarDay(date, false));
    }

    // Current month days
    for (let day = 1; day <= lastDay.getDate(); day++) {
      const date = new Date(year, month, day);
      this.calendarDays.push(this.createCalendarDay(date, true));
    }

    // Next month days
    const remainingDays = 42 - this.calendarDays.length;
    for (let day = 1; day <= remainingDays; day++) {
      const date = new Date(year, month + 1, day);
      this.calendarDays.push(this.createCalendarDay(date, false));
    }

    this.updateCalculations();
  }

  createCalendarDay(date: Date, isCurrentMonth: boolean): CalendarDay {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const dateStr = this.formatDateToLocal(date);
    const calendarDate = this.calendarData.find(d => d.date === dateStr);

    const status = calendarDate?.status ?? CalendarDateStatus.Available;
    const isPast = date < today;
    // Allow selecting dates from other months if they're not past and available
    const isSelectable = !isPast && status === CalendarDateStatus.Available;

    return {
      date,
      isCurrentMonth,
      isToday: date.getTime() === today.getTime(),
      isSelectable,
      isSelected: this.isDateSelected(date),
      isRangeStart: this.isRangeStart(date),
      isRangeEnd: this.isRangeEnd(date),
      isInRange: this.isInRange(date),
      status: isPast ? CalendarDateStatus.PastDate : status,
      statusDisplayName: calendarDate?.statusDisplayName || ''
    };
  }

  isDateSelected(date: Date): boolean {
    if (!this.selectedStartDate) return false;
    if (!this.selectedEndDate) return this.isSameDay(date, this.selectedStartDate);
    return this.isSameDay(date, this.selectedStartDate) || this.isSameDay(date, this.selectedEndDate);
  }

  isRangeStart(date: Date): boolean {
    return !!this.selectedStartDate && this.isSameDay(date, this.selectedStartDate);
  }

  isRangeEnd(date: Date): boolean {
    return !!this.selectedEndDate && this.isSameDay(date, this.selectedEndDate);
  }

  isInRange(date: Date): boolean {
    if (!this.selectedStartDate || !this.selectedEndDate) return false;
    return date > this.selectedStartDate && date < this.selectedEndDate;
  }

  isSameDay(date1: Date, date2: Date): boolean {
    return date1.getFullYear() === date2.getFullYear() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getDate() === date2.getDate();
  }

  onDayClick(day: CalendarDay): void {
    if (!day.isSelectable) {
      // Show message if trying to click unavailable day
      if (day.status !== CalendarDateStatus.Available && day.status !== CalendarDateStatus.PastDate) {
        this.messageService.add({
          severity: 'warn',
          summary: this.localization.instant('MP::DayUnavailable', 'Day Unavailable'),
          detail: this.localization.instant('MP::DayUnavailableDetail', `This day is ${day.statusDisplayName.toLowerCase()} and cannot be selected.`)
        });
      }
      return;
    }

    if (!this.selectedStartDate || (this.selectedStartDate && this.selectedEndDate)) {
      this.selectedStartDate = day.date;
      this.selectedEndDate = undefined;
      this.isSelectingRange = true;
      this.generateCalendar();
    } else {
      // User is selecting end date
      let tempStartDate = this.selectedStartDate;
      let tempEndDate = day.date;

      // Swap if end is before start
      if (tempEndDate < tempStartDate) {
        const swap = tempStartDate;
        tempStartDate = tempEndDate;
        tempEndDate = swap;
      }

      // Only validate minimum days here - GAP validation will happen in updateCalculations
      const days = Math.ceil((tempEndDate.getTime() - tempStartDate.getTime()) / (1000 * 60 * 60 * 24)) + 1;
      if (days < 7) {
        this.messageService.add({
          severity: 'warn',
          summary: this.localization.instant('MP::InvalidSelection', 'Invalid Selection'),
          detail: this.localization.instant('MP::MinimumRentalPeriod', 'Minimum rental period is 7 days')
        });
        return;
      }

      // Set the dates - GAP validation will happen in updateCalculations
      this.selectedStartDate = tempStartDate;
      this.selectedEndDate = tempEndDate;
      this.isSelectingRange = false;
      this.generateCalendar();
      this.updateCalculations();
    }
  }

  getDayClass(day: CalendarDay): string {
    const classes = ['calendar-day'];

    if (!day.isCurrentMonth) classes.push('other-month');
    if (day.isToday) classes.push('today');
    if (day.isSelectable) classes.push('selectable');

    // Add status classes FIRST so selection classes can override them
    switch (day.status) {
      case CalendarDateStatus.Available:
        classes.push('status-available');
        break;
      case CalendarDateStatus.Reserved:
        classes.push('status-reserved');
        break;
      case CalendarDateStatus.Occupied:
        classes.push('status-occupied');
        break;
      case CalendarDateStatus.Unavailable:
        classes.push('status-unavailable');
        break;
      case CalendarDateStatus.PastDate:
        classes.push('status-past');
        break;
    }

    // Add selection classes LAST so they have higher priority
    if (day.isInRange) classes.push('in-range');
    if (day.isSelected) classes.push('selected');
    if (day.isRangeStart) classes.push('range-start');
    if (day.isRangeEnd) classes.push('range-end');

    return classes.join(' ');
  }

  prevMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1, 1);
    this.loadCalendar();
  }

  nextMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 1);
    this.loadCalendar();
  }

  getMonthYear(): string {
    return this.currentDate.toLocaleDateString('default', { month: 'long', year: 'numeric' });
  }

  onBoothTypeChange(): void {
    this.updateCalculations();
  }

  updateCalculations(): void {
    this.hasGapError = false;

    if (this.selectedStartDate && this.selectedEndDate && this.booth) {
      const days = Math.ceil((this.selectedEndDate.getTime() - this.selectedStartDate.getTime()) / (1000 * 60 * 60 * 24)) + 1;

      // Calculate price first
      this.calculatedDays = days;
      this.calculatedPrice = days * this.booth.pricePerDay;

      // Validate gaps if minimum gap is set - but don't clear selection, just warn
      if (this.minimumGapDays > 0 && this.calendarData.length > 0) {
        const gapError = this.validateGaps(this.selectedStartDate, this.selectedEndDate, this.calendarData);
        if (gapError) {
          this.hasGapError = true;
          this.messageService.add({
            severity: 'error',
            summary: gapError.title,
            detail: gapError.message,
            life: 10000
          });
          // Don't clear the selection - let user see the issue and adjust
        }
      }
    } else {
      this.calculatedDays = 0;
      this.calculatedPrice = 0;
    }
  }

  validateGaps(startDate: Date, endDate: Date, calendarDates: CalendarDateDto[]): { title: string, message: string } | null {
    // Find nearest rental before start date
    const rentalsBefore = calendarDates
      .filter(d => {
        const date = new Date(d.date);
        return date < startDate && (d.status === CalendarDateStatus.Reserved || d.status === CalendarDateStatus.Occupied);
      })
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

    if (rentalsBefore.length > 0) {
      const lastRentalEndDate = new Date(rentalsBefore[0].date);
      const gapDays = Math.round((startDate.getTime() - lastRentalEndDate.getTime()) / (1000 * 60 * 60 * 24)) - 1;

      if (gapDays > 0 && gapDays < this.minimumGapDays) {
        const suggestedDate = new Date(lastRentalEndDate);
        suggestedDate.setDate(suggestedDate.getDate() + 1);
        const alternativeDate = new Date(lastRentalEndDate);
        alternativeDate.setDate(alternativeDate.getDate() + this.minimumGapDays + 1);

        return {
          title: this.localization.instant('MP::UnusableGapBeforeRental', 'Unusable Gap Before Rental'),
          message: `Your rental would leave a ${gapDays}-day gap before another rental. Minimum gap is ${this.minimumGapDays} days. Please start on ${suggestedDate.toLocaleDateString()} (adjacent) or ${alternativeDate.toLocaleDateString()} (with minimum gap).`
        };
      }
    }

    // Find nearest rental after end date
    const rentalsAfter = calendarDates
      .filter(d => {
        const date = new Date(d.date);
        return date > endDate && (d.status === CalendarDateStatus.Reserved || d.status === CalendarDateStatus.Occupied);
      })
      .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

    if (rentalsAfter.length > 0) {
      const nextRentalStartDate = new Date(rentalsAfter[0].date);
      const gapDays = Math.round((nextRentalStartDate.getTime() - endDate.getTime()) / (1000 * 60 * 60 * 24)) - 1;

      if (gapDays > 0 && gapDays < this.minimumGapDays) {
        const suggestedDate = new Date(nextRentalStartDate);
        suggestedDate.setDate(suggestedDate.getDate() - 1);
        const alternativeDate = new Date(nextRentalStartDate);
        alternativeDate.setDate(alternativeDate.getDate() - this.minimumGapDays - 1);

        return {
          title: this.localization.instant('MP::UnusableGapAfterRental', 'Unusable Gap After Rental'),
          message: `Your rental would leave a ${gapDays}-day gap after another rental. Minimum gap is ${this.minimumGapDays} days. Please end on ${suggestedDate.toLocaleDateString()} (adjacent) or ${alternativeDate.toLocaleDateString()} (with minimum gap).`
        };
      }
    }

    return null;
  }

  canSave(): boolean {
    return !!(this.selectedStartDate &&
              this.selectedEndDate &&
              this.selectedBoothType &&
              this.calculatedDays >= 7 &&
              !this.hasGapError);
  }

  save(): void {
    if (!this.canSave() || !this.selectedStartDate || !this.selectedEndDate || !this.selectedBoothType) {
      return;
    }

    this.saved.emit({
      boothTypeId: this.selectedBoothType.id,
      startDate: this.formatDateToLocal(this.selectedStartDate),
      endDate: this.formatDateToLocal(this.selectedEndDate),
      notes: this.notes
    });

    this.close();
  }

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  isValidDate(date: Date | undefined): boolean {
    return date instanceof Date && !isNaN(date.getTime());
  }

  trackByBoothTypeId(index: number, boothType: BoothTypeDto): string {
    return boothType.id;
  }

  trackByIndex(index: number, item: any): number {
    return index;
  }
}