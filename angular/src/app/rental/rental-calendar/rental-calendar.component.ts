import { Component, OnInit, OnDestroy, Input, Output, EventEmitter, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BoothService } from '../../services/booth.service';
import { BoothTypeService } from '../../services/booth-type.service';
import { RentalService } from '../../services/rental.service';
import { PaymentService } from '../../services/payment.service';
import { CartService } from '../../services/cart.service';
import { BoothSettingsService } from '../../services/booth-settings.service';
import { BoothSignalRService } from '../../services/booth-signalr.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { BoothDto } from '../../shared/models/booth.model';
import { BoothTypeDto } from '../../shared/models/booth-type.model';
import { CreateRentalDto, CreateRentalWithPaymentDto, CreateRentalWithPaymentResultDto, BoothCalendarRequestDto, BoothCalendarResponseDto, CalendarDateDto, CalendarDateStatus } from '../../shared/models/rental.model';
import { PaymentProvider, PaymentMethod, PaymentRequest } from '../../shared/models/payment.model';
import { AddToCartDto, UpdateCartItemDto, CartDto, CartItemDto } from '../../shared/models/cart.model';
import { MessageService } from 'primeng/api';
import { tap } from 'rxjs/operators';
import { firstValueFrom, Subscription } from 'rxjs';
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
  isInCart: boolean;

  // Calendar state from backend
  status: CalendarDateStatus;
  statusDisplayName: string;
  rentalId?: string;
  userName?: string;
  userEmail?: string;
  rentalStartDate?: Date;
  rentalEndDate?: Date;
  notes?: string;
}

@Component({
  selector: 'app-rental-calendar',
  templateUrl: './rental-calendar.component.html',
  styleUrls: ['./rental-calendar.component.scss'],
  standalone: false
})
export class RentalCalendarComponent implements OnInit, OnDestroy, OnChanges {
  // Input properties for reusability
  @Input() boothId?: string; // Can be provided from parent instead of route
  @Input() mode: 'user' | 'admin' = 'user'; // Display mode
  @Input() customMinDate?: Date; // Custom min date (for extension mode)
  @Input() customMaxDate?: Date; // Custom max date (for extension mode)
  @Input() hideBoothInfo = false; // Hide booth details card
  @Input() hideBoothTypeSelection = false; // Hide booth type selection
  @Input() hidePaymentActions = false; // Hide payment and cart actions
  @Input() hideCalendarNavigation = false; // Hide calendar navigation buttons
  @Input() hideLegend = false; // Hide calendar legend
  @Input() preselectedBoothTypeId?: string; // Preselect booth type (for admin mode)
  @Input() excludeCartId?: string; // Exclude specific cart ID from calendar validation (for cart editing)

  // Output properties
  @Output() datesSelected = new EventEmitter<{startDate: Date, endDate: Date, isValid: boolean}>();
  @Output() validationError = new EventEmitter<string | null>();
  @Output() costCalculated = new EventEmitter<{cost: number, days: number}>();
  @Output() paymentRequested = new EventEmitter<{provider: any, method?: any}>();
  @Output() paymentDialogStateChange = new EventEmitter<boolean>();

  booth?: BoothDto;
  boothTypes: BoothTypeDto[] = [];
  selectedBoothType?: BoothTypeDto;

  currentDate = new Date();
  calendarDays: CalendarDay[] = [];

  // Calendar data from backend
  calendarData?: BoothCalendarResponseDto;
  calendarLegend: { [key: string]: string } = {};
  minimumGapDays = 7; // Default value, will be loaded from settings
  minimumRentalDays = 7; // Default value, will be loaded from settings

  // Expose enum for template
  CalendarDateStatus = CalendarDateStatus;

  // Expose Object for template
  Object = Object;

  selectedStartDate?: Date;
  selectedEndDate?: Date;
  isSelectingRange = false;

  calculatedPrice = 0;
  calculatedDays = 0;
  commissionPercentage = 0;

  loading = false;
  processingPayment = false;
  hasGapError = false;
  gapErrorMessage = '';
  tenantCurrencyCode: string = 'PLN';

  // Payment selection
  showPaymentSelection = false;
  selectedPaymentProvider?: PaymentProvider;
  selectedPaymentMethod?: PaymentMethod;
  createdRental?: any;
  createdRentalId?: string; // Backup ID in case object is lost

  // DEBUG: Track how dialog was opened
  debugDialogOpenReason = 'none';

  // Cart state
  currentCart: CartDto | null = null;
  private cartSubscription?: Subscription;
  private boothUpdatesSubscription?: Subscription;
  currentCartItems: CartItemDto[] = []; // All cart items for this booth
  currentCartItem?: CartItemDto; // Currently selected cart item for editing
  selectedCartItemId?: string; // ID of currently selected/active cart item for visual highlighting
  isEditingCartItem = false; // Flag to track if we're editing existing cart item
  notes = ''; // Notes/special requests for the booking

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private boothService: BoothService,
    private boothTypeService: BoothTypeService,
    private rentalService: RentalService,
    private paymentService: PaymentService,
    public cartService: CartService,
    private boothSignalRService: BoothSignalRService,
    private messageService: MessageService,
    private boothSettingsService: BoothSettingsService,
    private tenantCurrencyService: TenantCurrencyService,
    private localization: LocalizationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    console.log('RentalCalendar: ngOnInit called, component initialized');
    console.log('RentalCalendar: Mode:', this.mode);
    console.log('RentalCalendar: BoothId from Input:', this.boothId);

    // Load booth settings for gap validation
    this.loadBoothSettings();

    // Load tenant currency
    this.tenantCurrencyService.getCurrency().subscribe(result => {
      this.tenantCurrencyCode = this.tenantCurrencyService.getCurrencyName(result.currency);
    });

    // Subscribe to cart changes (only in user mode)
    if (this.mode === 'user') {
      this.cartSubscription = this.cartService.cart$.subscribe(cart => {
        this.currentCart = cart;
        // Check if this booth is in cart and load its data
        this.loadCartItemData();
        // Regenerate calendar when cart changes to update visual indicators
        if (this.boothId && this.calendarDays.length > 0) {
          this.generateCalendar();
        }
      });
    }

    // Subscribe to booth status updates via SignalR
    console.log('RentalCalendar: Setting up booth updates subscription for boothId:', this.boothId);
    this.boothUpdatesSubscription = this.boothSignalRService.boothUpdates.subscribe(update => {
      console.log('RentalCalendar: ✅ Received booth status update via SignalR:', update);
      // Reload calendar data if the update is for the current booth
      if (this.boothId === update.boothId) {
        console.log('RentalCalendar: Update is for current booth - regenerating calendar...');
        this.generateCalendar();
      } else {
        console.log('RentalCalendar: Update is for different booth (current:', this.boothId, 'updated:', update.boothId, ') - ignoring');
      }
    });
    console.log('RentalCalendar: ✅ Booth updates subscription active');

    // If boothId not provided via Input, try to get it from route (backward compatibility)
    if (!this.boothId) {
      this.boothId = this.route.snapshot.paramMap.get('boothId') || undefined;
    }

    if (this.boothId) {
      this.loadBooth();
      this.loadBoothTypes();
      this.generateCalendar();

      // Check if payment dialog should be opened from URL params (only in user mode)
      if (this.mode === 'user' && this.route.snapshot.queryParams['payment'] === 'true') {
        console.log('🚨 RentalCalendar: Payment dialog requested from URL - this might be the problem!');
        this.showPaymentSelection = true;
        this.debugDialogOpenReason = 'URL_PARAM';
      }
    } else {
      // If no boothId and in user mode, redirect back to rental booth selection
      if (this.mode === 'user') {
        this.router.navigate(['/rentals']);
      }
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('RentalCalendar: ngOnChanges called', changes);

    // React to changes in Input properties
    if (changes['boothId'] && this.boothId) {
      console.log('RentalCalendar: boothId changed to:', this.boothId);
      // Load booth data when boothId changes (including first time)
      this.loadBooth();
      this.loadBoothTypes();
      this.generateCalendar();
      this.cdr.detectChanges();
    }

    if (changes['preselectedBoothTypeId'] && this.preselectedBoothTypeId) {
      console.log('RentalCalendar: preselectedBoothTypeId changed to:', this.preselectedBoothTypeId);
      // Find and select the preselected booth type
      const boothType = this.boothTypes.find(bt => bt.id === this.preselectedBoothTypeId);
      if (boothType) {
        this.selectedBoothType = boothType;
        this.validateAndCalculatePrice();
        this.cdr.detectChanges();
      }
    }
  }

  ngOnDestroy(): void {
    console.log('RentalCalendar: ngOnDestroy called, component destroyed');
    if (this.cartSubscription) {
      this.cartSubscription.unsubscribe();
    }
    if (this.boothUpdatesSubscription) {
      this.boothUpdatesSubscription.unsubscribe();
    }
  }

  loadBooth(): void {
    if (!this.boothId) return;

    this.loading = true;
    this.boothService.get(this.boothId).subscribe({
      next: (booth) => {
        this.booth = booth;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading booth:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load booth details'
        });
        this.loading = false;
      }
    });
  }

  loadBoothSettings(): void {
    this.boothSettingsService.get().subscribe({
      next: (settings) => {
        this.minimumGapDays = settings.minimumGapDays;
        this.minimumRentalDays = settings.minimumRentalDays;
      },
      error: (error) => {
        console.error('Error loading booth settings:', error);
        // Use default value (7) if settings can't be loaded
      }
    });
  }

  loadBoothTypes(): void {
    this.boothTypeService.getList({ skipCount: 0, maxResultCount: 100 }).subscribe({
      next: (result) => {
        this.boothTypes = result.items.filter(bt => bt.isActive);
        // After loading booth types, check if we need to set booth type from cart
        this.loadCartItemData();
      },
      error: (error) => {
        console.error('Error loading booth types:', error);
      }
    });
  }

  generateCalendar(): void {
    console.log('RentalCalendar: generateCalendar called, boothId:', this.boothId);
    if (!this.boothId) {
      console.warn('RentalCalendar: generateCalendar aborted - no boothId');
      return;
    }

    const year = this.currentDate.getFullYear();
    const month = this.currentDate.getMonth();

    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);
    const firstDayOfCalendar = new Date(firstDayOfMonth);
    firstDayOfCalendar.setDate(firstDayOfCalendar.getDate() - firstDayOfCalendar.getDay());

    const lastDayOfCalendar = new Date(lastDayOfMonth);
    const remainingDays = 6 - lastDayOfMonth.getDay();
    lastDayOfCalendar.setDate(lastDayOfCalendar.getDate() + remainingDays);

    console.log('RentalCalendar: Calendar range:', { firstDay: firstDayOfCalendar, lastDay: lastDayOfCalendar });

    // Load calendar data from backend
    this.loadCalendarData(firstDayOfCalendar, lastDayOfCalendar, month);
  }

  private loadCalendarData(startDate: Date, endDate: Date, month: number): void {
    console.log('RentalCalendar: loadCalendarData called, boothId:', this.boothId);
    if (!this.boothId) {
      console.warn('RentalCalendar: loadCalendarData aborted - no boothId');
      return;
    }

    const request: BoothCalendarRequestDto = {
      boothId: this.boothId,
      startDate: this.formatDateForApi(startDate),
      endDate: this.formatDateForApi(endDate),
      excludeCartId: this.excludeCartId // Exclude specific cart from validation (for cart editing)
    };

    console.log('RentalCalendar: Fetching calendar data with request:', request);

    this.rentalService.getBoothCalendar(request).subscribe({
      next: (response) => {
        console.log('RentalCalendar: Calendar data received:', response);
        this.calendarData = response;
        this.calendarLegend = response.legend;
        this.buildCalendarDays(startDate, endDate, month, response.dates);
      },
      error: (error) => {
        console.error('RentalCalendar: Error loading calendar data:', error);
        // Fallback to client-side calendar generation
        this.buildCalendarDaysWithoutBackendData(startDate, endDate, month);
      }
    });
  }

  private buildCalendarDays(startDate: Date, endDate: Date, month: number, backendDates: CalendarDateDto[]): void {
    console.log('RentalCalendar: buildCalendarDays called', { startDate, endDate, backendDatesCount: backendDates.length });
    this.calendarDays = [];
    const currentDate = new Date(startDate);

    // Create a lookup map for backend data
    const dateMap = new Map<string, CalendarDateDto>();
    backendDates.forEach(dateDto => {
      dateMap.set(dateDto.date, dateDto);
    });

    while (currentDate <= endDate) {
      const dateKey = this.formatDateForApi(currentDate);
      const backendData = dateMap.get(dateKey);

      const day: CalendarDay = {
        date: new Date(currentDate),
        isCurrentMonth: currentDate.getMonth() === month,
        isToday: this.isSameDay(currentDate, new Date()),
        isSelectable: this.isDateSelectable(currentDate, backendData),
        isSelected: this.isDateSelected(currentDate),
        isRangeStart: this.isRangeStart(currentDate),
        isRangeEnd: this.isRangeEnd(currentDate),
        isInRange: this.isInRange(currentDate),
        isInCart: this.isDateInCart(currentDate),

        // Backend data
        status: backendData?.status ?? CalendarDateStatus.Available,
        statusDisplayName: backendData?.statusDisplayName ?? 'Available',
        rentalId: backendData?.rentalId,
        userName: backendData?.userName,
        userEmail: backendData?.userEmail,
        rentalStartDate: backendData?.rentalStartDate ? new Date(backendData.rentalStartDate) : undefined,
        rentalEndDate: backendData?.rentalEndDate ? new Date(backendData.rentalEndDate) : undefined,
        notes: backendData?.notes
      };

      this.calendarDays.push(day);
      currentDate.setDate(currentDate.getDate() + 1);
    }

    console.log('RentalCalendar: buildCalendarDays completed, days count:', this.calendarDays.length);
    this.cdr.detectChanges();
  }

  private buildCalendarDaysWithoutBackendData(startDate: Date, endDate: Date, month: number): void {
    console.log('RentalCalendar: buildCalendarDaysWithoutBackendData called (fallback)');
    this.calendarDays = [];
    const currentDate = new Date(startDate);

    while (currentDate <= endDate) {
      const day: CalendarDay = {
        date: new Date(currentDate),
        isCurrentMonth: currentDate.getMonth() === month,
        isToday: this.isSameDay(currentDate, new Date()),
        isSelectable: this.isDateSelectable(currentDate),
        isSelected: this.isDateSelected(currentDate),
        isRangeStart: this.isRangeStart(currentDate),
        isRangeEnd: this.isRangeEnd(currentDate),
        isInRange: this.isInRange(currentDate),
        isInCart: this.isDateInCart(currentDate),

        // Default backend data
        status: this.isDateSelectable(currentDate) ? CalendarDateStatus.Available : CalendarDateStatus.PastDate,
        statusDisplayName: this.isDateSelectable(currentDate) ? 'Available' : 'Past Date'
      };

      this.calendarDays.push(day);
      currentDate.setDate(currentDate.getDate() + 1);
    }

    console.log('RentalCalendar: buildCalendarDaysWithoutBackendData completed, days count:', this.calendarDays.length);
    this.cdr.detectChanges();
  }

  private formatDateForApi(date: Date): string {
    // Use local timezone to prevent date shifting due to UTC conversion
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  /**
   * Check if a given date is within any cart item date range for current booth
   */
  private isDateInCart(date: Date): boolean {
    if (!this.currentCart || !this.currentCart.items || !this.boothId) {
      return false;
    }

    // Filter cart items for this booth only
    const boothCartItems = this.currentCart.items.filter(item => item.boothId === this.boothId);

    // Check if date falls within any cart item's date range
    return boothCartItems.some(item => {
      const itemStart = new Date(item.startDate);
      itemStart.setHours(0, 0, 0, 0);
      const itemEnd = new Date(item.endDate);
      itemEnd.setHours(0, 0, 0, 0);
      const checkDate = new Date(date);
      checkDate.setHours(0, 0, 0, 0);

      return checkDate >= itemStart && checkDate <= itemEnd;
    });
  }

  /**
   * Load all cart items for current booth
   * Sorts by reservation expiry (active first, then expired) and then by start date
   */
  private loadCartItemData(): void {
    if (!this.currentCart || !this.currentCart.items || !this.boothId) {
      this.currentCartItems = [];
      this.currentCartItem = undefined;
      this.isEditingCartItem = false;
      return;
    }

    // Find ALL cart items for this booth (not just first one)
    this.currentCartItems = this.currentCart.items
      .filter(item => item.boothId === this.boothId)
      .sort((a, b) => {
        // Sort by reservation expiry (active first, then expired)
        const now = new Date();
        const aActive = a.reservationExpiresAt && new Date(a.reservationExpiresAt) > now;
        const bActive = b.reservationExpiresAt && new Date(b.reservationExpiresAt) > now;

        if (aActive && !bActive) return -1;
        if (!aActive && bActive) return 1;

        // Then by start date
        return new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
      });

    // If no current item selected and we have items, don't auto-select
    // Let user explicitly choose which one to edit
    if (!this.currentCartItem && this.currentCartItems.length > 0) {
      // Just clear the editing flag
      this.isEditingCartItem = false;
    }
  }

  /**
   * Select cart item for editing - loads its data into calendar
   * Makes this item active (highlighted) and dimms other items
   */
  editCartItem(item: CartItemDto): void {
    // Set this item as selected
    this.selectedCartItemId = item.id;

    // Set dates and booth type from selected item
    this.selectedStartDate = new Date(item.startDate);
    this.selectedEndDate = new Date(item.endDate);
    this.notes = item.notes || '';

    const boothType = this.boothTypes.find(bt => bt.id === item.boothTypeId);
    if (boothType) {
      this.selectedBoothType = boothType;
    }

    // Mark as editing this specific item
    this.currentCartItem = item;
    this.isEditingCartItem = true;

    // Regenerate calendar to show selection
    this.generateCalendar();
    this.validateAndCalculatePrice();

    // Force change detection to update UI
    this.cdr.detectChanges();
  }

  /**
   * Cancel editing - deselect cart item
   */
  cancelEditingCartItem(): void {
    this.selectedCartItemId = undefined;
    this.currentCartItem = undefined;
    this.isEditingCartItem = false;
    this.clearSelection();
    this.cdr.detectChanges();
  }

  /**
   * Check if given cart item is currently selected/active
   */
  isCartItemSelected(item: CartItemDto): boolean {
    return this.selectedCartItemId === item.id;
  }

  /**
   * Check if given cart item should be dimmed (when another item is selected)
   */
  isCartItemDimmed(item: CartItemDto): boolean {
    return !!this.selectedCartItemId && this.selectedCartItemId !== item.id;
  }

  /**
   * Remove specific cart item
   */
  removeCartItem(item: CartItemDto): void {
    this.cartService.removeItem(item.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Removed from Cart',
          detail: `Reservation for ${new Date(item.startDate).toLocaleDateString()} - ${new Date(item.endDate).toLocaleDateString()} removed`,
          life: 3000
        });

        // If we were editing this item, clear the selection
        if (this.currentCartItem?.id === item.id) {
          this.clearSelection();
          this.currentCartItem = undefined;
          this.isEditingCartItem = false;
        }

        // Cart will auto-update via subscription
      },
      error: (error) => {
        console.error('Error removing cart item:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to remove item from cart'
        });
      }
    });
  }

  /**
   * Start new reservation (clear selection)
   */
  startNewReservation(): void {
    // Clear current selection
    this.clearSelection();
    this.currentCartItem = undefined;
    this.isEditingCartItem = false;

    // Scroll to calendar
    setTimeout(() => {
      const calendarElement = document.querySelector('.calendar-container');
      if (calendarElement) {
        calendarElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }
    }, 100);

    this.messageService.add({
      severity: 'info',
      summary: 'New Reservation',
      detail: 'Select dates in calendar to add new period',
      life: 3000
    });
  }

  /**
   * Get countdown display for cart item (reuse from cart.component logic)
   */
  getCountdown(itemId: string): string {
    const item = this.currentCartItems.find(i => i.id === itemId);
    if (!item || !item.reservationExpiresAt) {
      return '';
    }

    const now = new Date();
    const expiresAt = new Date(item.reservationExpiresAt);
    const diff = expiresAt.getTime() - now.getTime();

    if (diff <= 0) {
      return '00:00';
    }

    const minutes = Math.floor(diff / 60000);
    const seconds = Math.floor((diff % 60000) / 1000);
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  }

  /**
   * Get count of active (non-expired) reservations
   */
  get activeReservationsCount(): number {
    const now = new Date();
    return this.currentCartItems.filter(item =>
      item.reservationExpiresAt && new Date(item.reservationExpiresAt) > now
    ).length;
  }

  /**
   * Get count of expired reservations
   */
  get expiredReservationsCount(): number {
    const now = new Date();
    return this.currentCartItems.filter(item =>
      item.reservationExpiresAt && new Date(item.reservationExpiresAt) <= now
    ).length;
  }

  /**
   * TrackBy function for cart items list
   */
  trackByCartItemId(index: number, item: CartItemDto): string {
    return item.id;
  }

  /**
   * Check if cart item has active (non-expired) reservation
   */
  isReservationActive(item: CartItemDto): boolean {
    if (!item.reservationExpiresAt) return false;
    const now = new Date();
    const expiresAt = new Date(item.reservationExpiresAt);
    return expiresAt > now;
  }

  /**
   * Check if cart item has expired reservation
   */
  isReservationExpired(item: CartItemDto): boolean {
    if (!item.reservationExpiresAt) return false;
    const now = new Date();
    const expiresAt = new Date(item.reservationExpiresAt);
    return expiresAt <= now;
  }

  isDateSelectable(date: Date, backendData?: CalendarDateDto): boolean {
    // Check custom min/max dates first
    if (this.customMinDate) {
      const minDate = new Date(this.customMinDate);
      minDate.setHours(0, 0, 0, 0);
      const checkDate = new Date(date);
      checkDate.setHours(0, 0, 0, 0);
      if (checkDate < minDate) {
        return false;
      }
    }

    if (this.customMaxDate) {
      const maxDate = new Date(this.customMaxDate);
      maxDate.setHours(0, 0, 0, 0);
      const checkDate = new Date(date);
      checkDate.setHours(0, 0, 0, 0);
      if (checkDate > maxDate) {
        return false;
      }
    }

    // If we have backend data, use that for selectability
    if (backendData) {
      return backendData.status === CalendarDateStatus.Available;
    }

    // Fallback: Can't select dates in the past - rentals can start from today
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return date >= today;
  }

  isDateSelected(date: Date): boolean {
    return (this.selectedStartDate && this.isSameDay(date, this.selectedStartDate)) ||
           (this.selectedEndDate && this.isSameDay(date, this.selectedEndDate));
  }

  isRangeStart(date: Date): boolean {
    return this.selectedStartDate ? this.isSameDay(date, this.selectedStartDate) : false;
  }

  isRangeEnd(date: Date): boolean {
    return this.selectedEndDate ? this.isSameDay(date, this.selectedEndDate) : false;
  }

  isInRange(date: Date): boolean {
    if (!this.selectedStartDate || !this.selectedEndDate) return false;
    return date > this.selectedStartDate && date < this.selectedEndDate;
  }

  isSameDay(date1: Date, date2: Date): boolean {
    return date1.getDate() === date2.getDate() &&
           date1.getMonth() === date2.getMonth() &&
           date1.getFullYear() === date2.getFullYear();
  }

  /**
   * Get all unavailable dates in the specified range
   * @param startDate Start date of range (inclusive)
   * @param endDate End date of range (inclusive)
   * @returns Array of unavailable dates with their status information
   */
  getUnavailableDatesInRange(startDate: Date, endDate: Date): Array<{ date: Date; status: CalendarDateStatus; statusDisplayName: string }> {
    const unavailableDates: Array<{ date: Date; status: CalendarDateStatus; statusDisplayName: string }> = [];
    const currentDate = new Date(startDate);
    currentDate.setHours(0, 0, 0, 0);
    const end = new Date(endDate);
    end.setHours(0, 0, 0, 0);

    while (currentDate <= end) {
      // Find the calendar day for this date
      const calendarDay = this.calendarDays.find(day => this.isSameDay(day.date, currentDate));

      if (calendarDay) {
        // Check if this date is not available
        if (calendarDay.status !== CalendarDateStatus.Available) {
          unavailableDates.push({
            date: new Date(currentDate),
            status: calendarDay.status,
            statusDisplayName: calendarDay.statusDisplayName
          });
        }
      } else {
        // If date is not in current calendar view, we need to check against backend data
        const dateKey = this.formatDateForApi(currentDate);
        const backendDate = this.calendarData?.dates.find(d => d.date === dateKey);

        if (backendDate && backendDate.status !== CalendarDateStatus.Available) {
          unavailableDates.push({
            date: new Date(currentDate),
            status: backendDate.status,
            statusDisplayName: backendDate.statusDisplayName
          });
        }
      }

      currentDate.setDate(currentDate.getDate() + 1);
    }

    return unavailableDates;
  }

  onDayClick(day: CalendarDay): void {
    console.log('RentalCalendar: onDayClick called', day.date);

    if (!day.isSelectable || day.status !== CalendarDateStatus.Available) {
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
      // Start new selection - validate start date for gaps
      const proposedStartDate = new Date(day.date);

      // Check if this start date would create an invalid gap
      if (this.minimumGapDays > 0 && this.calendarData) {
        const gapValidation = this.validateStartDateGap(proposedStartDate);
        if (gapValidation) {
          // Invalid gap detected - auto-correct to adjacent date
          this.messageService.add({
            severity: 'warn',
            summary: gapValidation.title,
            detail: gapValidation.message,
            life: 8000
          });

          // Auto-correct to the suggested adjacent date
          this.selectedStartDate = gapValidation.suggestedDate;
          this.selectedEndDate = undefined;
          this.isSelectingRange = true;
          this.generateCalendar();
          this.cdr.detectChanges();
          return;
        }
      }

      // Valid start date - proceed
      this.selectedStartDate = proposedStartDate;
      this.selectedEndDate = undefined;
      this.isSelectingRange = true;
      this.generateCalendar();
      this.cdr.detectChanges(); // Force change detection
    } else if (this.selectedStartDate && !this.selectedEndDate) {
      // User is selecting end date
      let tempStartDate = this.selectedStartDate;
      let tempEndDate = new Date(day.date);

      // Swap if end is before start
      if (tempEndDate < tempStartDate) {
        const swap = tempStartDate;
        tempStartDate = tempEndDate;
        tempEndDate = swap;
      }

      // Validate minimum days
      const millisecondsPerDay = 1000 * 60 * 60 * 24;
      const days = Math.round((tempEndDate.getTime() - tempStartDate.getTime()) / millisecondsPerDay) + 1;

      if (days < this.minimumRentalDays) {
        this.messageService.add({
          severity: 'warn',
          summary: this.localization.instant('MP::InvalidSelection', 'Invalid Selection'),
          detail: this.localization.instant('MP::MinimumRentalPeriod', `Minimum rental period is ${this.minimumRentalDays} days`)
        });
        return;
      }

      // Validate that all dates in range are available
      const unavailableDates = this.getUnavailableDatesInRange(tempStartDate, tempEndDate);
      if (unavailableDates.length > 0) {
        const dateList = unavailableDates
          .slice(0, 5) // Show max 5 dates
          .map(d => d.date.toLocaleDateString())
          .join(', ');
        const moreCount = unavailableDates.length - 5;
        const dateListText = moreCount > 0 ? `${dateList} (+${moreCount} more)` : dateList;

        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('MP::RangeContainsUnavailableDates', 'Range Contains Unavailable Dates'),
          detail: this.localization.instant('MP::RangeContainsUnavailableDatesDetail',
            `The selected date range contains ${unavailableDates.length} unavailable day(s): ${dateListText}. Please select a different period.`),
          life: 8000
        });
        return;
      }

      // Check if end date would create an invalid gap with the next rental
      if (this.minimumGapDays > 0 && this.calendarData) {
        const endGapValidation = this.validateEndDateGap(tempEndDate);
        if (endGapValidation) {
          // Invalid gap detected - auto-correct to adjacent date
          this.messageService.add({
            severity: 'warn',
            summary: endGapValidation.title,
            detail: endGapValidation.message,
            life: 8000
          });

          // Auto-correct to the suggested adjacent date
          tempEndDate = endGapValidation.suggestedDate;
        }
      }

      // Set the dates
      this.selectedStartDate = tempStartDate;
      this.selectedEndDate = tempEndDate;
      this.isSelectingRange = false;
      this.generateCalendar();
      this.validateAndCalculatePrice();
      this.cdr.detectChanges(); // Force change detection
    }
  }

  validateAndCalculatePrice(): void {
    this.hasGapError = false;
    this.gapErrorMessage = '';

    if (!this.selectedStartDate || !this.selectedEndDate || !this.booth) return;

    // Calculate days correctly: end date - start date + 1 (inclusive of both days)
    const millisecondsPerDay = 1000 * 60 * 60 * 24;
    const startTime = this.selectedStartDate.getTime();
    const endTime = this.selectedEndDate.getTime();
    const days = Math.round((endTime - startTime) / millisecondsPerDay) + 1;

    // Calculate price first
    this.calculatedDays = days;
    this.calculatedPrice = this.calculatePriceWithGreedyAlgorithm(days);

    if (this.selectedBoothType) {
      this.commissionPercentage = this.selectedBoothType.commissionPercentage;
    }

    // Emit cost calculation
    this.costCalculated.emit({
      cost: this.calculatedPrice,
      days: this.calculatedDays
    });

    // Validate gaps if minimum gap is set - but don't clear selection, just warn
    if (this.minimumGapDays > 0 && this.calendarData) {
      const gapError = this.validateGaps(this.selectedStartDate, this.selectedEndDate, this.calendarData.dates);
      if (gapError) {
        this.hasGapError = true;
        this.gapErrorMessage = gapError.message;
        this.validationError.emit(gapError.message);
        this.messageService.add({
          severity: 'error',
          summary: gapError.title,
          detail: gapError.message,
          life: 10000
        });
        // Don't clear the selection - let user see the issue and adjust
      } else {
        this.validationError.emit(null);
      }
    } else {
      this.validationError.emit(null);
    }

    // Emit dates selection with validation status
    this.datesSelected.emit({
      startDate: this.selectedStartDate,
      endDate: this.selectedEndDate,
      isValid: !this.hasGapError && this.calculatedDays >= this.minimumRentalDays
    });
  }

  /**
   * Validate if a proposed start date creates an invalid gap with the previous rental
   * Returns validation result with suggested correction date
   */
  validateStartDateGap(startDate: Date): { title: string, message: string, suggestedDate: Date } | null {
    if (!this.calendarData) return null;

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    // Find nearest rental before start date
    const rentalsBefore = this.calendarData.dates
      .filter(d => {
        const date = new Date(d.date);
        return date < startDate && (d.status === CalendarDateStatus.Reserved || d.status === CalendarDateStatus.Occupied);
      })
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

    if (rentalsBefore.length > 0) {
      const lastRentalEndDate = new Date(rentalsBefore[0].date);
      lastRentalEndDate.setHours(0, 0, 0, 0);

      // If the previous rental ended in the past, don't enforce gap validation
      if (lastRentalEndDate >= today) {
        const gapDays = Math.round((startDate.getTime() - lastRentalEndDate.getTime()) / (1000 * 60 * 60 * 24)) - 1;

        if (gapDays > 0 && gapDays < this.minimumGapDays) {
          const suggestedDate = new Date(lastRentalEndDate);
          suggestedDate.setDate(suggestedDate.getDate() + 1); // Adjacent date
          const alternativeDate = new Date(lastRentalEndDate);
          alternativeDate.setDate(alternativeDate.getDate() + this.minimumGapDays + 1);

          return {
            title: this.localization.instant('MP::UnusableGapBeforeRental', 'Unusable Gap Before Rental'),
            message: `Your rental would leave a ${gapDays}-day gap after the previous rental (ends ${lastRentalEndDate.toLocaleDateString()}). Minimum gap is ${this.minimumGapDays} days. Auto-corrected to ${suggestedDate.toLocaleDateString()} (adjacent). Alternative: ${alternativeDate.toLocaleDateString()} (with minimum gap).`,
            suggestedDate: suggestedDate
          };
        }
      }
    }

    return null;
  }

  /**
   * Validate if a proposed end date creates an invalid gap with the next rental
   * Returns validation result with suggested correction date
   */
  validateEndDateGap(endDate: Date): { title: string, message: string, suggestedDate: Date } | null {
    if (!this.calendarData) return null;

    // Find nearest rental after end date
    const rentalsAfter = this.calendarData.dates
      .filter(d => {
        const date = new Date(d.date);
        return date > endDate && (d.status === CalendarDateStatus.Reserved || d.status === CalendarDateStatus.Occupied);
      })
      .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

    if (rentalsAfter.length > 0) {
      const nextRentalStartDate = new Date(rentalsAfter[0].date);
      nextRentalStartDate.setHours(0, 0, 0, 0);

      const gapDays = Math.round((nextRentalStartDate.getTime() - endDate.getTime()) / (1000 * 60 * 60 * 24)) - 1;

      if (gapDays > 0 && gapDays < this.minimumGapDays) {
        const suggestedDate = new Date(nextRentalStartDate);
        suggestedDate.setDate(suggestedDate.getDate() - 1); // Adjacent date
        const alternativeDate = new Date(nextRentalStartDate);
        alternativeDate.setDate(alternativeDate.getDate() - this.minimumGapDays - 1);

        return {
          title: this.localization.instant('MP::UnusableGapAfterRental', 'Unusable Gap After Rental'),
          message: `Your rental would leave a ${gapDays}-day gap before the next rental (starts ${nextRentalStartDate.toLocaleDateString()}). Minimum gap is ${this.minimumGapDays} days. Auto-corrected to ${suggestedDate.toLocaleDateString()} (adjacent). Alternative: ${alternativeDate.toLocaleDateString()} (with minimum gap).`,
          suggestedDate: suggestedDate
        };
      }
    }

    return null;
  }

  validateGaps(startDate: Date, endDate: Date, calendarDates: CalendarDateDto[]): { title: string, message: string } | null {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    // Find nearest rental before start date
    const rentalsBefore = calendarDates
      .filter(d => {
        const date = new Date(d.date);
        return date < startDate && (d.status === CalendarDateStatus.Reserved || d.status === CalendarDateStatus.Occupied);
      })
      .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());

    if (rentalsBefore.length > 0) {
      const lastRentalEndDate = new Date(rentalsBefore[0].date);
      lastRentalEndDate.setHours(0, 0, 0, 0);

      // If the previous rental ended in the past, don't enforce gap validation
      // The gap has already been "wasted" by the passage of time
      if (lastRentalEndDate >= today) {
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

  onBoothTypeChange(): void {
    if (this.selectedBoothType && this.selectedStartDate && this.selectedEndDate) {
      this.validateAndCalculatePrice();
    }
  }

  nextMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + 1, 1);
    this.generateCalendar();
    this.cdr.detectChanges();
  }

  prevMonth(): void {
    this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() - 1, 1);
    this.generateCalendar();
    this.cdr.detectChanges();
  }

  goToToday(): void {
    this.currentDate = new Date();
    this.generateCalendar();
    this.cdr.detectChanges();
  }

  /**
   * Calculate price using greedy algorithm with multi-period pricing
   * Example: 10 days with [1day=1zł, 3days=2zł, 7days=6zł]
   * Result: 1×7days + 1×3days = 1×6zł + 1×2zł = 8zł (not 10×1zł = 10zł)
   */
  private calculatePriceWithGreedyAlgorithm(totalDays: number): number {
    if (!this.booth || !this.booth.pricingPeriods || this.booth.pricingPeriods.length === 0) {
      // Fallback to legacy pricePerDay if no pricing periods
      return totalDays * this.booth.pricePerDay;
    }

    // Sort periods by days descending (greedy: use largest first)
    const sortedPeriods = [...this.booth.pricingPeriods].sort((a, b) => b.days - a.days);

    let remainingDays = totalDays;
    let totalPrice = 0;

    // Greedy algorithm: use largest periods first
    for (const period of sortedPeriods) {
      const count = Math.floor(remainingDays / period.days);
      if (count > 0) {
        totalPrice += count * period.pricePerPeriod;
        remainingDays -= count * period.days;
      }

      if (remainingDays === 0) {
        break;
      }
    }

    // If there are remaining days not covered by any period, use smallest period's daily rate
    if (remainingDays > 0) {
      const smallestPeriod = sortedPeriods[sortedPeriods.length - 1];
      const pricePerDay = smallestPeriod.pricePerPeriod / smallestPeriod.days;
      totalPrice += remainingDays * pricePerDay;
    }

    return totalPrice;
  }

  /**
   * Check if price breakdown should be shown (when using multi-period pricing)
   */
  hasPriceBreakdown(): boolean {
    return !!(this.booth?.pricingPeriods && this.booth.pricingPeriods.length > 1 && this.calculatedDays > 0);
  }

  /**
   * Get formatted HTML tooltip with price breakdown
   */
  getPriceBreakdownTooltip(): string {
    if (!this.booth || !this.booth.pricingPeriods || this.booth.pricingPeriods.length === 0 || !this.calculatedDays) {
      return '';
    }

    const sortedPeriods = [...this.booth.pricingPeriods].sort((a, b) => b.days - a.days);
    let remainingDays = this.calculatedDays;
    let html = '<div class="price-breakdown-tooltip">';
    html += '<div class="breakdown-title"><strong>Rozliczenie ceny:</strong></div>';

    // Greedy algorithm breakdown
    for (const period of sortedPeriods) {
      const count = Math.floor(remainingDays / period.days);
      if (count > 0) {
        const subtotal = count * period.pricePerPeriod;
        const dayLabel = period.days === 1 ? 'dzień' : 'dni';
        html += '<div class="breakdown-item">';
        html += `• ${count} × ${period.days} ${dayLabel} = ${this.formatCurrency(subtotal)} (${this.formatCurrency(period.pricePerPeriod)} każdy)`;
        html += '</div>';
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
      html += '<div class="breakdown-item">';
      html += `• ${remainingDays} ${dayLabel} × ${this.formatCurrency(pricePerDay)}/dzień = ${this.formatCurrency(subtotal)}`;
      html += '</div>';
    }

    html += '<hr class="breakdown-divider">';
    html += `<div class="breakdown-total"><strong>Suma: ${this.formatCurrency(this.calculatedPrice)}</strong></div>`;
    html += '</div>';

    return html;
  }

  /**
   * Format currency value
   */
  private formatCurrency(value: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: this.tenantCurrencyCode || 'PLN'
    }).format(value);
  }

  /**
   * Get pricing periods display for Booth Details section
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
   * Get price breakdown HTML for "Complete Your Booking" section
   * Shows detailed breakdown when using multi-period pricing
   * Returns HTML like "2× 7 dni (4 zł)<br>1 dzień (1 zł)"
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

  clearSelection(): void {
    this.selectedStartDate = undefined;
    this.selectedEndDate = undefined;
    this.isSelectingRange = false;
    this.calculatedPrice = 0;
    this.calculatedDays = 0;
    this.notes = ''; // Clear notes
    this.generateCalendar();
    this.cdr.detectChanges();
  }

  canProceedToPayment(): boolean {
    return !!(this.selectedStartDate &&
              this.selectedEndDate &&
              this.selectedBoothType &&
              this.booth &&
              this.calculatedDays >= this.minimumRentalDays &&
              !this.hasGapError);
  }

  async addOrUpdateCart(): Promise<void> {
    if (!this.canProceedToPayment() || !this.boothId) {
      return;
    }

    // Check if we're updating existing cart item or adding new one
    if (this.isEditingCartItem && this.currentCartItem) {
      // Update existing cart item
      const updateCartItemDto: UpdateCartItemDto = {
        boothTypeId: this.selectedBoothType!.id,
        startDate: this.formatDateForApi(this.selectedStartDate!),
        endDate: this.formatDateForApi(this.selectedEndDate!),
        notes: this.notes // Use current notes from form
      };

      this.cartService.updateItem(this.currentCartItem.id, updateCartItemDto).subscribe({
        next: (cart) => {
          this.messageService.add({
            severity: 'success',
            summary: 'Cart Updated',
            detail: `Booth ${this.booth?.number} updated in cart`,
            life: 3000
          });

          // Don't clear selection - keep it for further edits
        },
        error: (error) => {
          console.error('Error updating cart:', error);
          this.handleCartError(error);
        }
      });
    } else {
      // Add new item to cart
      const addToCartDto: AddToCartDto = {
        boothId: this.boothId,
        boothTypeId: this.selectedBoothType!.id,
        startDate: this.formatDateForApi(this.selectedStartDate!),
        endDate: this.formatDateForApi(this.selectedEndDate!),
        notes: this.notes // Use notes from form
      };

      this.cartService.addItem(addToCartDto).subscribe({
        next: (cart) => {
          this.messageService.add({
            severity: 'success',
            summary: 'Added to Cart',
            detail: `Booth ${this.booth?.number} added to cart`,
            life: 3000
          });

          // Clear selection after adding
          this.clearSelection();
        },
        error: (error) => {
          console.error('Error adding to cart:', error);
          this.handleCartError(error);
        }
      });
    }
  }

  private handleCartError(error: any): void {
    const errorCode = error.error?.error?.code;
    const errorData = error.error?.error?.data;

    if (errorCode === 'RENTAL_CREATES_UNUSABLE_GAP_BEFORE') {
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
        summary: 'Error',
        detail: error.error?.error?.message || 'Failed to add/update cart'
      });
    }
  }

  async onPaymentSelected(selection: {provider: PaymentProvider, method?: PaymentMethod}): Promise<void> {
    console.log('RentalCalendar: onPaymentSelected called with selection:', selection);
    console.log('RentalCalendar: Will now create rental AND payment atomically');

    if (!this.canProceedToPayment() || !this.boothId) {
      console.error('RentalCalendar: Cannot proceed - invalid state');
      return;
    }

    this.selectedPaymentProvider = selection.provider;
    this.selectedPaymentMethod = selection.method;
    this.showPaymentSelection = false;
    this.processingPayment = true;

    try {
      // Create rental with payment in one atomic operation
      const createRentalWithPaymentDto: CreateRentalWithPaymentDto = {
        boothId: this.boothId,
        boothTypeId: this.selectedBoothType!.id,
        startDate: this.formatDateForApi(this.selectedStartDate!),
        endDate: this.formatDateForApi(this.selectedEndDate!),
        paymentProviderId: selection.provider.id,
        paymentMethodId: selection.method?.id
      };

      console.log('RentalCalendar: Creating rental with payment:', createRentalWithPaymentDto);

      const result = await firstValueFrom(
        this.rentalService.createMyRentalWithPayment(createRentalWithPaymentDto).pipe(
          tap(result => console.log('RentalCalendar: Rental with payment result:', result))
        )
      );

      if (result.success && result.paymentUrl) {
        console.log('RentalCalendar: Success! Redirecting to payment URL:', result.paymentUrl);

        // Store rental info for potential recovery
        if (result.rentalId) {
          localStorage.setItem('currentRental', JSON.stringify({
            id: result.rentalId,
            transactionId: result.transactionId,
            timestamp: Date.now()
          }));
        }

        // Redirect to payment provider
        window.location.href = result.paymentUrl;
      } else {
        console.error('RentalCalendar: Payment creation failed:', result.errorMessage);
        this.messageService.add({
          severity: 'error',
          summary: 'Payment Error',
          detail: result.errorMessage || 'Failed to create payment. Please try again.'
        });
        this.processingPayment = false;
      }

    } catch (error) {
      console.error('RentalCalendar: Error creating rental with payment:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Failed to create rental with payment. Please try again.'
      });
      this.processingPayment = false;
    }
  }

  onPaymentCancelled(): void {
    this.showPaymentSelection = false;
    // Optionally delete the created rental if user cancels payment
    // this.deleteCreatedRental();
  }

  private async processPayment(): Promise<void> {
    console.log('ProcessPayment: Called with state:', {
      createdRental: this.createdRental,
      selectedPaymentProvider: this.selectedPaymentProvider,
      selectedPaymentMethod: this.selectedPaymentMethod,
      showPaymentSelection: this.showPaymentSelection,
      processingPayment: this.processingPayment
    });

    if (!this.createdRental || !this.selectedPaymentProvider) {
      console.error('ProcessPayment: Missing required data', {
        createdRental: this.createdRental,
        selectedPaymentProvider: this.selectedPaymentProvider
      });
      return;
    }

    console.log('ProcessPayment: Starting payment processing...', {
      createdRental: this.createdRental,
      selectedPaymentProvider: this.selectedPaymentProvider,
      selectedPaymentMethod: this.selectedPaymentMethod
    });

    this.processingPayment = true;

    try {
      const paymentRequest: PaymentRequest = {
        amount: this.calculatedPrice,
        currency: this.tenantCurrencyCode,
        description: `Booth rental ${this.booth?.number} from ${this.selectedStartDate?.toLocaleDateString()} to ${this.selectedEndDate?.toLocaleDateString()}`,
        providerId: this.selectedPaymentProvider.id,
        methodId: this.selectedPaymentMethod?.id,
        metadata: {
          rentalId: this.createdRental.id,
          boothId: this.boothId,
          boothTypeId: this.selectedBoothType?.id
        }
      };

      console.log('ProcessPayment: Sending payment request:', paymentRequest);

      const response = await this.paymentService.createPayment(paymentRequest).toPromise();

      console.log('ProcessPayment: Received response:', response);

      if (response && response.success && response.paymentUrl) {
        console.log('ProcessPayment: Redirecting to payment URL:', response.paymentUrl);

        // Clear localStorage backup since payment flow is starting
        localStorage.removeItem('currentRental');
        console.log('ProcessPayment: Cleared localStorage backup');

        // Redirect to payment provider
        window.location.href = response.paymentUrl;
      } else {
        console.error('ProcessPayment: Payment creation failed:', response);
        throw new Error(response?.errorMessage || 'Payment creation failed');
      }

    } catch (error) {
      console.error('ProcessPayment: Error processing payment:', error);
      this.messageService.add({
        severity: 'error',
        summary: 'Payment Error',
        detail: 'Failed to initiate payment. Please try again.'
      });
      this.processingPayment = false;
    }
  }

  getMonthYear(): string {
    return this.currentDate.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  getDayClass(day: CalendarDay): string {
    const classes = ['calendar-day'];

    if (!day.isCurrentMonth) classes.push('other-month');
    if (day.isToday) classes.push('today');
    if (!day.isSelectable) classes.push('disabled');

    // Add cart indicator (highest priority for visual feedback)
    if (day.isInCart) {
      classes.push('in-cart');
    }

    // Add status-specific classes
    switch (day.status) {
      case CalendarDateStatus.Available:
        classes.push('available');
        break;
      case CalendarDateStatus.Reserved:
        classes.push('reserved');
        break;
      case CalendarDateStatus.Occupied:
        classes.push('occupied');
        break;
      case CalendarDateStatus.Unavailable:
        classes.push('unavailable');
        break;
      case CalendarDateStatus.PastDate:
        classes.push('past-date');
        break;
      case CalendarDateStatus.Historical:
        classes.push('historical');
        break;
    }

    // Selection-related classes LAST (only for available dates) - higher priority
    if (day.status === CalendarDateStatus.Available) {
      if (day.isInRange) classes.push('in-range');
      if (day.isSelected) classes.push('selected');
      if (day.isRangeStart) classes.push('range-start');
      if (day.isRangeEnd) classes.push('range-end');
    }

    return classes.join(' ');
  }

  getDateTooltip(day: CalendarDay): string {
    let tooltip = day.date.toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });

    if (day.isInCart) {
      tooltip += ' - In Cart';
    }

    if (day.status !== CalendarDateStatus.Available) {
      tooltip += ` - ${day.statusDisplayName}`;
    }

    if (day.userName) {
      tooltip += ` (Rented by: ${day.userName})`;
    }

    if (day.rentalStartDate && day.rentalEndDate) {
      tooltip += ` [${day.rentalStartDate.toLocaleDateString()} - ${day.rentalEndDate.toLocaleDateString()}]`;
    }

    return tooltip;
  }

  getCalendarStatuses(): string[] {
    return Object.keys(this.calendarLegend);
  }

  getLegendClass(statusKey: string): string {
    const status = Number(statusKey) as CalendarDateStatus;
    switch (status) {
      case CalendarDateStatus.Available:
        return 'legend-available';
      case CalendarDateStatus.Reserved:
        return 'legend-reserved';
      case CalendarDateStatus.Occupied:
        return 'legend-occupied';
      case CalendarDateStatus.Unavailable:
        return 'legend-unavailable';
      case CalendarDateStatus.PastDate:
        return 'legend-past-date';
      case CalendarDateStatus.Historical:
        return 'legend-historical';
      default:
        return '';
    }
  }

  trackByBoothTypeId(index: number, boothType: BoothTypeDto): string {
    return boothType.id;
  }

  trackByIndex(index: number, item: any): number {
    return index;
  }

  trackByStatusKey(index: number, statusKey: string): string {
    return statusKey;
  }
}