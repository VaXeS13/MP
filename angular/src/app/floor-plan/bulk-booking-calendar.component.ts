import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { forkJoin, Subject, Subscription } from 'rxjs';
import { share } from 'rxjs/operators';
import { takeUntil, finalize, debounceTime } from 'rxjs/operators';
import { addMonths, startOfMonth, endOfMonth, format, addDays, isAfter, isBefore, startOfDay, eachDayOfInterval, isSameDay } from 'date-fns';
import { pl } from 'date-fns/locale';
import { Canvas, Rect, Text, Group, Shadow, FabricImage } from 'fabric';

import { FloorPlanService } from '../services/floor-plan.service';
import { RentalService } from '../services/rental.service';
import { BoothService } from '../services/booth.service';
import { BoothTypeService } from '../services/booth-type.service';
import { BoothSettingsService, BoothSettingsDto } from '../services/booth-settings.service';
import { CartService } from '../services/cart.service';
import { TenantCurrencyService } from '../services/tenant-currency.service';
import { BoothSignalRService, BoothStatusUpdate } from '../services/booth-signalr.service';

import { FloorPlanDto, FloorPlanBoothDto } from '../shared/models/floor-plan.model';
import { BoothCalendarRequestDto, BoothCalendarResponseDto, CalendarDateStatus } from '../shared/models/rental.model';
import { AddToCartDto, CartDto } from '../shared/models/cart.model';
import { BoothDto } from '../shared/models/booth.model';
import { BoothTypeDto } from '../shared/models/booth-type.model';

export interface CalendarGridCell {
  date: Date;
  boothId: string;
  boothNumber: string;
  dayOfMonth: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  isAvailable: boolean;
  isPastDate: boolean;
  isSelected: boolean;
  isRangeStart: boolean;
  isRangeEnd: boolean;
  isInRange: boolean;
  isInCart: boolean; // Check if date is in current cart
  isInSelectedBookings: boolean; // NEW: Check if date is in selected bookings list
  status: CalendarDateStatus;
  statusDisplayName: string;
  // Additional properties from backend for detailed tracking
  rentalId?: string;
  userName?: string;
  userEmail?: string;
  rentalStartDate?: Date;
  rentalEndDate?: Date;
  notes?: string;
}

export interface BoothCalendarData {
  boothId: string;
  boothNumber: string;
  boothDto?: BoothDto;
  responseData?: BoothCalendarResponseDto;
  cells: CalendarGridCell[];
  selectedStartDate?: Date;
  selectedEndDate?: Date;
  isLoading: boolean;
  loadError?: string;
}

export interface PriceBreakdownItem {
  daysCount: number;
  price: number;
  label: string; // e.g., "7 dni (5,00 zł)" or "4 x 1 dzień (8,00 zł)"
}

export interface SelectedBooking {
  boothId: string;
  boothNumber: string;
  startDate: Date;
  endDate: Date;
  daysCount: number;
  pricePerDay: number;
  totalPrice: number;
  currency: string;
  priceBreakdown?: PriceBreakdownItem[]; // NEW: Detailed price breakdown by tiers
  boothTypeId?: string; // NEW: Selected booth type ID
}

@Component({
  selector: 'app-bulk-booking-calendar',
  templateUrl: './bulk-booking-calendar.component.html',
  styleUrls: ['./bulk-booking-calendar.component.scss'],
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BulkBookingCalendarComponent implements OnInit, AfterViewInit, OnDestroy {
  floorPlanId?: string;
  floorPlan?: FloorPlanDto;
  floorPlans: FloorPlanDto[] = [];
  booths: FloorPlanBoothDto[] = [];
  boothsData: Map<string, BoothDto> = new Map();
  isLoadingFloorPlans = true;

  // Calendar state
  currentDateRange: { start: Date; end: Date } = {
    start: startOfMonth(new Date()),
    end: endOfMonth(addMonths(new Date(), 1))
  };

  calendarData: Map<string, BoothCalendarData> = new Map();
  allCalendarDays: Date[] = [];

  // Booth settings
  boothSettings?: BoothSettingsDto;
  minimumRentalDays = 7;
  minimumGapDays = 7;

  // Cart state
  currentCart: CartDto | null = null;
  private cartSubscription?: Subscription;

  // Legend data
  calendarLegend: { [key: string]: string } = {};

  // Selection state
  selectedBookings: SelectedBooking[] = [];
  currentSelectionBoothId?: string;
  isSelectingRange = false;

  // UI State
  isLoading = false;
  totalLoading = false;
  isSaving = false;
  tenantCurrencyCode: string = 'PLN';

  validationErrors: string[] = [];

  // Booth type selection
  boothTypes: BoothTypeDto[] = [];
  selectedBoothTypeId?: string; // Selected booth type for current booking

  // Floor plan visualization
  selectedBoothFloorPlans: FloorPlanDto[] = [];
  floorPlanCanvases: Map<string, Canvas> = new Map();
  showFloorPlanSection = false;
  @ViewChild('floorPlanCanvasContainer', { static: false }) floorPlanContainer?: ElementRef;

  private destroy$ = new Subject<void>();
  private selectionSubject$ = new Subject<void>();
  private boothUpdatesSubscription?: Subscription;

  // Expose enums for template
  CalendarDateStatus = CalendarDateStatus;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private floorPlanService: FloorPlanService,
    private rentalService: RentalService,
    private boothService: BoothService,
    private boothTypeService: BoothTypeService,
    private boothSettingsService: BoothSettingsService,
    private cartService: CartService,
    private tenantCurrencyService: TenantCurrencyService,
    private boothSignalRService: BoothSignalRService,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initializeComponent();
  }

  ngAfterViewInit(): void {
    // Initialize floor plan canvases if floor plans are already loaded
    if (this.selectedBoothFloorPlans.length > 0 && this.floorPlanContainer) {
      setTimeout(() => {
        this.initializeFloorPlanCanvases();
      });
    }
  }

  ngOnDestroy(): void {
    // Clean up floor plan canvases
    this.clearFloorPlans();

    // Unsubscribe from floor plan updates
    if (this.floorPlanId) {
      this.boothSignalRService.unsubscribeFromFloorPlan(this.floorPlanId);
    }

    // Unsubscribe from booth status updates
    this.boothUpdatesSubscription?.unsubscribe();

    // Unsubscribe from cart updates
    this.cartSubscription?.unsubscribe();

    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeComponent(): void {
    // Load list of all floor plans
    this.loadFloorPlans();

    // Load booth types
    this.boothTypeService.getList({ skipCount: 0, maxResultCount: 100 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.boothTypes = result.items || [];
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Error loading booth types:', error);
          // Continue anyway - booth type selection is optional
        }
      });

    // Load currency
    this.tenantCurrencyService.getCurrency().pipe(takeUntil(this.destroy$)).subscribe(result => {
      this.tenantCurrencyCode = this.tenantCurrencyService.getCurrencyName(result.currency);
      this.cdr.markForCheck();
    });

    // Load booth settings
    this.boothSettingsService.get().pipe(takeUntil(this.destroy$)).subscribe({
      next: (settings) => {
        this.boothSettings = settings;
        this.minimumRentalDays = settings.minimumRentalDays;
        this.minimumGapDays = settings.minimumGapDays;
        this.cdr.markForCheck();
      }
    });

    // Subscribe to cart changes
    this.cartSubscription = this.cartService.cart$.subscribe(cart => {
      this.currentCart = cart;
      // Regenerate calendar when cart changes to update visual indicators
      if (this.calendarData.size > 0) {
        this.updateCartIndicators();
        this.cdr.markForCheck();
      }
    });

    // Setup selection debounce
    this.selectionSubject$.pipe(
      debounceTime(300),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.validateSelection();
    });
  }

  private loadFloorPlans(): void {
    this.isLoadingFloorPlans = true;
    this.cdr.markForCheck();

    this.floorPlanService.getListByTenant()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (plans: FloorPlanDto[]) => {
          this.floorPlans = plans.sort((a, b) => (a.name || '').localeCompare(b.name || ''));
          this.isLoadingFloorPlans = false;

          // Load booths from all floor plans
          this.loadAllBooths();

          this.cdr.markForCheck();
        },
        error: (error) => {
          this.isLoadingFloorPlans = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować planów sal'
          });
          this.cdr.markForCheck();
        }
      });
  }

  selectFloorPlan(floorPlanId?: string): void {
    if (!floorPlanId) {
      // Show all booths when null/empty selected
      this.floorPlanId = undefined;
      this.floorPlan = undefined;
      this.loadAllBooths();
    } else {
      // Show booths from selected plan
      this.floorPlanId = floorPlanId;
      this.loadFloorPlanAndBooths();
    }
  }

  private loadAllBooths(): void {
    this.isLoading = true;
    this.totalLoading = true;
    this.cdr.markForCheck();

    // Load all booths from the system (not limited by floor plans)
    this.boothService.getList({ skipCount: 0, maxResultCount: 1000 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          // Convert BoothListDto[] to FloorPlanBoothDto[] format for compatibility
          this.booths = (result.items || [])
            .map(booth => ({
              boothId: booth.id,
              booth: booth as any,
              id: booth.id,
              floorPlanId: '',
              x: 0,
              y: 0,
              width: 0,
              height: 0,
              rotation: 0,
              creationTime: new Date()
            } as FloorPlanBoothDto))
            .sort((a, b) => {
              const aNum = a.booth?.number || '';
              const bNum = b.booth?.number || '';
              return aNum.localeCompare(bNum);
            });

          // Clear floor plan selection when showing all
          this.floorPlan = undefined;

          this.loadCalendarData();
        },
        error: (error) => {
          this.isLoading = false;
          this.totalLoading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować stanowisk'
          });
          this.cdr.markForCheck();
        }
      });
  }

  private loadFloorPlanAndBooths(): void {
    if (!this.floorPlanId) return;

    this.isLoading = true;
    this.totalLoading = true;
    this.cdr.markForCheck();

    this.floorPlanService.get(this.floorPlanId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (floorPlan) => {
          this.floorPlan = floorPlan;
          this.loadBooths();
        },
        error: (error) => {
          this.isLoading = false;
          this.totalLoading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować planu piętra'
          });
          this.cdr.markForCheck();
        }
      });
  }

  private loadBooths(): void {
    if (!this.floorPlanId) return;

    this.floorPlanService.getBooths(this.floorPlanId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (booths) => {
          this.booths = booths.sort((a, b) => {
            const aNum = a.booth?.number || '';
            const bNum = b.booth?.number || '';
            return aNum.localeCompare(bNum);
          });
          this.loadCalendarData();
        },
        error: (error) => {
          this.isLoading = false;
          this.totalLoading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować stanowisk'
          });
          this.cdr.markForCheck();
        }
      });
  }

  private loadCalendarData(): void {
    if (this.booths.length === 0) {
      this.isLoading = false;
      this.totalLoading = false;
      this.cdr.markForCheck();
      return;
    }

    const startDateStr = format(this.currentDateRange.start, 'yyyy-MM-dd');
    const endDateStr = format(this.currentDateRange.end, 'yyyy-MM-dd');

    // Prepare all calendar day cells
    this.allCalendarDays = eachDayOfInterval({
      start: this.currentDateRange.start,
      end: this.currentDateRange.end
    });

    // Load calendar data for all booths in parallel
    const calendarRequests = this.booths.map(booth => {
      const request: BoothCalendarRequestDto = {
        boothId: booth.boothId,
        startDate: startDateStr,
        endDate: endDateStr
      };

      return this.rentalService.getBoothCalendar(request).pipe(
        finalize(() => {
          this.cdr.markForCheck();
        })
      );
    });

    forkJoin(calendarRequests)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (responses) => {
          this.processCalendarResponses(responses);
          this.setupSignalRSubscriptions();
          this.isLoading = false;
          this.totalLoading = false;
          this.cdr.markForCheck();
        },
        error: (error) => {
          this.isLoading = false;
          this.totalLoading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować kalendarza dostępności'
          });
          this.cdr.markForCheck();
        }
      });
  }

  private processCalendarResponses(responses: BoothCalendarResponseDto[]): void {
    this.calendarData.clear();

    responses.forEach((response) => {
      const boothFloorPlan = this.booths.find(b => b.boothId === response.boothId);
      if (!boothFloorPlan || !boothFloorPlan.booth) return;

      const booth = boothFloorPlan.booth;
      const cells: CalendarGridCell[] = this.allCalendarDays.map(day => {
        const dateStr = format(day, 'yyyy-MM-dd');
        const calendarDate = response.dates.find(d => d.date === dateStr);

        return {
          date: day,
          boothId: booth.id,
          boothNumber: booth.number,
          dayOfMonth: day.getDate(),
          isCurrentMonth: isSameDay(day, startOfDay(day)) && day >= this.currentDateRange.start && day <= this.currentDateRange.end,
          isToday: isSameDay(day, new Date()),
          isAvailable: calendarDate?.status === CalendarDateStatus.Available,
          isPastDate: calendarDate?.status === CalendarDateStatus.PastDate || calendarDate?.status === CalendarDateStatus.Historical,
          isSelected: false,
          isRangeStart: false,
          isRangeEnd: false,
          isInRange: false,
          isInCart: this.isDateInCart(booth.id, day), // Check if date is in cart
          isInSelectedBookings: this.isDateInSelectedBookings(booth.id, day), // Check if date is in selected bookings list
          status: calendarDate?.status ?? CalendarDateStatus.Unavailable,
          statusDisplayName: calendarDate?.statusDisplayName ?? 'Niedostępne',
          rentalId: calendarDate?.rentalId,
          userName: calendarDate?.userName,
          userEmail: calendarDate?.userEmail,
          rentalStartDate: calendarDate?.rentalStartDate ? new Date(calendarDate.rentalStartDate) : undefined,
          rentalEndDate: calendarDate?.rentalEndDate ? new Date(calendarDate.rentalEndDate) : undefined,
          notes: calendarDate?.notes
        };
      });

      const boothData: BoothCalendarData = {
        boothId: booth.id,
        boothNumber: booth.number,
        boothDto: booth,
        responseData: response,
        cells,
        isLoading: false
      };

      // Store legend from backend response
      if (response.legend) {
        this.calendarLegend = response.legend;
      }

      this.calendarData.set(booth.id, boothData);
    });
  }

  private setupSignalRSubscriptions(): void {
    if (!this.floorPlanId) return;

    // Subscribe to floor plan updates
    this.boothSignalRService.subscribeToFloorPlan(this.floorPlanId);

    // Subscribe to booth status changes
    this.boothUpdatesSubscription = this.boothSignalRService.boothUpdates
      .pipe(takeUntil(this.destroy$))
      .subscribe((update: BoothStatusUpdate) => {
        this.handleBoothStatusUpdate(update);
      });
  }

  private handleBoothStatusUpdate(update: BoothStatusUpdate): void {
    console.log('Booth status updated:', update);
    // Reload calendar data to reflect the booth status change
    this.loadCalendarData();
  }

  // Calendar Navigation
  previousMonth(): void {
    this.currentDateRange = {
      start: startOfMonth(addMonths(this.currentDateRange.start, -1)),
      end: endOfMonth(addMonths(this.currentDateRange.end, -1))
    };
    this.loadCalendarData();
  }

  nextMonth(): void {
    this.currentDateRange = {
      start: startOfMonth(addMonths(this.currentDateRange.start, 1)),
      end: endOfMonth(addMonths(this.currentDateRange.end, 1))
    };
    this.loadCalendarData();
  }

  getCurrentMonths(): string {
    const format1 = format(this.currentDateRange.start, 'MMMM yyyy', { locale: pl });
    const format2 = format(this.currentDateRange.end, 'MMMM yyyy', { locale: pl });
    return `${format1.charAt(0).toUpperCase() + format1.slice(1)} - ${format2.charAt(0).toUpperCase() + format2.slice(1)}`;
  }

  // Cell interaction
  onCellClick(cell: CalendarGridCell): void {
    // Block clicking on dates that are in selected bookings list
    if (cell.isInSelectedBookings) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Już na liście',
        detail: 'Ta data jest już na liście wybranych rezerwacji. Usuń z listy, aby wybrać inny zakres.'
      });
      return;
    }

    // Block clicking on dates that are in cart
    if (cell.isInCart) {
      this.messageService.add({
        severity: 'warn',
        summary: 'W koszyku',
        detail: 'Ta data jest już w koszyku. Usuń z koszyka, aby wybrać inny zakres.'
      });
      return;
    }

    if (cell.isPastDate || !cell.isAvailable) {
      return;
    }

    const boothData = this.calendarData.get(cell.boothId);
    if (!boothData) return;

    // Start new selection for this booth
    if (!this.currentSelectionBoothId || this.currentSelectionBoothId !== cell.boothId) {
      this.clearCurrentSelection();
      this.currentSelectionBoothId = cell.boothId;
      // Load floor plans containing this booth
      this.loadFloorPlansForBooth(cell.boothId);
    }

    if (!boothData.selectedStartDate) {
      // Set start date
      boothData.selectedStartDate = cell.date;
      this.updateCellSelection(boothData);
    } else if (!boothData.selectedEndDate) {
      // Set end date
      if (isAfter(cell.date, boothData.selectedStartDate) || isSameDay(cell.date, boothData.selectedStartDate)) {
        boothData.selectedEndDate = cell.date;
        this.updateCellSelection(boothData);
        this.selectionSubject$.next();
      } else {
        // If clicked date is before start, reset
        boothData.selectedStartDate = cell.date;
        boothData.selectedEndDate = undefined;
        this.updateCellSelection(boothData);
      }
    } else {
      // Both dates selected, start new selection
      this.clearCurrentSelection();
      boothData.selectedStartDate = cell.date;
      boothData.selectedEndDate = undefined;
      this.updateCellSelection(boothData);
    }

    this.cdr.markForCheck();
  }

  private clearCurrentSelection(): void {
    if (this.currentSelectionBoothId) {
      const boothData = this.calendarData.get(this.currentSelectionBoothId);
      if (boothData) {
        boothData.selectedStartDate = undefined;
        boothData.selectedEndDate = undefined;
        this.updateCellSelection(boothData);
      }
    }
    this.currentSelectionBoothId = undefined;
    this.selectedBoothTypeId = undefined;
    this.validationErrors = [];
    // Clear floor plans when selection is cleared
    this.clearFloorPlans();
  }

  private updateCellSelection(boothData: BoothCalendarData): void {
    boothData.cells.forEach(cell => {
      cell.isSelected = false;
      cell.isRangeStart = false;
      cell.isRangeEnd = false;
      cell.isInRange = false;

      if (boothData.selectedStartDate) {
        if (isSameDay(cell.date, boothData.selectedStartDate)) {
          cell.isRangeStart = true;
          cell.isSelected = true;
        }

        if (boothData.selectedEndDate) {
          if (isSameDay(cell.date, boothData.selectedEndDate)) {
            cell.isRangeEnd = true;
            cell.isSelected = true;
          }

          if (isAfter(cell.date, boothData.selectedStartDate) && isBefore(cell.date, boothData.selectedEndDate)) {
            cell.isInRange = true;
            cell.isSelected = true;
          }
        }
      }
    });
  }

  private validateSelection(): void {
    this.validationErrors = [];

    if (!this.currentSelectionBoothId) {
      return;
    }

    const boothData = this.calendarData.get(this.currentSelectionBoothId);
    if (!boothData || !boothData.selectedStartDate || !boothData.selectedEndDate) {
      return;
    }

    const daysCount = this.calculateDaysCount(boothData.selectedStartDate, boothData.selectedEndDate);

    // Check minimum rental days
    if (daysCount < this.minimumRentalDays) {
      this.validationErrors.push(
        `Minimalny okres wynajmu to ${this.minimumRentalDays} dni (wybrano ${daysCount})`
      );
    }

    // Check for unavailable dates or dates in cart within the selected range
    const hasUnavailable = boothData.cells.some(cell => {
      // Check if cell is within the selected range (inclusive of start and end dates)
      const isInRange = (
        (isAfter(cell.date, boothData.selectedStartDate!) || isSameDay(cell.date, boothData.selectedStartDate!)) &&
        (isBefore(cell.date, boothData.selectedEndDate!) || isSameDay(cell.date, boothData.selectedEndDate!))
      );

      return isInRange && (!cell.isAvailable || cell.isInCart);
    });

    if (hasUnavailable) {
      this.validationErrors.push('Wybrane daty zawierają zajęte dni lub daty z koszyka');
    }

    // Check gaps between reservations if minimum gap is set
    if (this.minimumGapDays > 0) {
      const gapErrors = this.validateGaps(boothData, boothData.selectedStartDate, boothData.selectedEndDate);
      this.validationErrors.push(...gapErrors);
    }

    this.cdr.markForCheck();
  }

  addToSelectedBookings(): void {
    if (!this.currentSelectionBoothId) {
      return;
    }

    const boothData = this.calendarData.get(this.currentSelectionBoothId);
    if (!boothData || !boothData.selectedStartDate || !boothData.selectedEndDate) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Nie wybrano dat',
        detail: 'Wybierz zakres dat dla stanowiska'
      });
      return;
    }

    if (this.validationErrors.length > 0) {
      this.messageService.add({
        severity: 'error',
        summary: 'Błędy walidacji',
        detail: this.validationErrors[0]
      });
      return;
    }

    // Validate booth type selection if types are available
    if (this.boothTypes.length > 0 && !this.selectedBoothTypeId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Typ stanowiska wymagany',
        detail: 'Wybierz typ stanowiska zanim dodasz do listy'
      });
      return;
    }

    if (!boothData.boothDto) return;

    const daysCount = this.calculateDaysCount(boothData.selectedStartDate, boothData.selectedEndDate);
    // Use greedy algorithm for price calculation with breakdown
    const priceResult = this.calculatePriceWithGreedyAlgorithm(boothData.boothDto, daysCount);
    const totalPrice = priceResult.totalPrice;
    // Calculate effective price per day based on tiered pricing
    const pricePerDay = daysCount > 0 ? totalPrice / daysCount : boothData.boothDto.pricePerDay;

    // Check if already in selection
    const existing = this.selectedBookings.find(
      b => b.boothId === this.currentSelectionBoothId &&
           isSameDay(b.startDate, boothData.selectedStartDate!) &&
           isSameDay(b.endDate, boothData.selectedEndDate!)
    );

    if (existing) {
      this.messageService.add({
        severity: 'info',
        summary: 'Już dodane',
        detail: 'To stanowisko z tym zakresem dat już znajduje się na liście'
      });
      return;
    }

    const booking: SelectedBooking = {
      boothId: this.currentSelectionBoothId,
      boothNumber: boothData.boothNumber,
      startDate: boothData.selectedStartDate,
      endDate: boothData.selectedEndDate,
      daysCount,
      pricePerDay,
      totalPrice,
      currency: this.tenantCurrencyCode,
      priceBreakdown: priceResult.breakdown,
      boothTypeId: this.selectedBoothTypeId
    };

    this.selectedBookings.push(booking);
    this.messageService.add({
      severity: 'success',
      summary: 'Dodane',
      detail: `Stanowisko ${boothData.boothNumber} dodane do listy`
    });

    // Update visual indicators - mark dates as in selected bookings
    this.updateSelectedBookingsIndicators();

    // Clear selection
    this.clearCurrentSelection();
    this.cdr.markForCheck();
  }

  removeSelectedBooking(index: number): void {
    const booking = this.selectedBookings[index];
    this.selectedBookings.splice(index, 1);
    this.messageService.add({
      severity: 'info',
      summary: 'Usunięto',
      detail: `Stanowisko ${booking.boothNumber} usunięte`
    });

    // Update visual indicators - unblock dates that were in this booking
    this.updateSelectedBookingsIndicators();

    this.cdr.markForCheck();
  }

  addAllToCart(): void {
    if (this.selectedBookings.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Lista pusta',
        detail: 'Dodaj stanowiska do listy zanim dodasz je do koszyka'
      });
      return;
    }

    this.isSaving = true;
    this.cdr.markForCheck();

    // Add items to cart sequentially
    const bookingsToAdd = [...this.selectedBookings];
    const addItemsSequentially = (index: number) => {
      if (index >= bookingsToAdd.length) {
        this.isSaving = false;
        this.selectedBookings = [];
        this.messageService.add({
          severity: 'success',
          summary: 'Sukces',
          detail: `Dodano ${bookingsToAdd.length} stanowisk do koszyka`
        });
        this.cdr.markForCheck();
        return;
      }

      const booking = bookingsToAdd[index];
      const boothFloorPlan = this.booths.find(b => b.boothId === booking.boothId);
      if (!boothFloorPlan || !boothFloorPlan.booth) {
        addItemsSequentially(index + 1);
        return;
      }

      const booth = boothFloorPlan.booth;
      const cartItem: AddToCartDto = {
        boothId: booking.boothId,
        boothTypeId: booking.boothTypeId || boothFloorPlan.id, // Use selected booth type if available
        startDate: format(booking.startDate, 'yyyy-MM-dd'),
        endDate: format(booking.endDate, 'yyyy-MM-dd'),
        notes: undefined
      };

      this.cartService.addItem(cartItem).pipe(takeUntil(this.destroy$)).subscribe({
        next: () => {
          addItemsSequentially(index + 1);
        },
        error: (error) => {
          this.isSaving = false;
          const errorMessage = error.error?.message || 'Nie udało się dodać do koszyka';
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: errorMessage
          });
          this.cdr.markForCheck();
        }
      });
    };

    addItemsSequentially(0);
  }

  getTotalPrice(): number {
    return this.selectedBookings.reduce((sum, booking) => sum + booking.totalPrice, 0);
  }

  calculateDaysCount(startDate: Date, endDate: Date): number {
    const diffTime = endDate.getTime() - startDate.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return Math.max(1, diffDays + 1);
  }

  formatDate(date: Date): string {
    return format(date, 'dd.MM.yyyy', { locale: pl });
  }

  /**
   * Check if a given date is within any cart item date range for specific booth
   */
  private isDateInCart(boothId: string, date: Date): boolean {
    if (!this.currentCart || !this.currentCart.items) {
      return false;
    }

    // Filter cart items for this booth only
    const boothCartItems = this.currentCart.items.filter(item => item.boothId === boothId);

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
   * Check if a given date is in selected bookings list for specific booth
   */
  private isDateInSelectedBookings(boothId: string, date: Date): boolean {
    return this.selectedBookings.some(booking => {
      if (booking.boothId !== boothId) return false;

      const bookingStart = new Date(booking.startDate);
      bookingStart.setHours(0, 0, 0, 0);
      const bookingEnd = new Date(booking.endDate);
      bookingEnd.setHours(0, 0, 0, 0);
      const checkDate = new Date(date);
      checkDate.setHours(0, 0, 0, 0);

      return checkDate >= bookingStart && checkDate <= bookingEnd;
    });
  }

  /**
   * Update cart indicators for all calendar cells
   */
  private updateCartIndicators(): void {
    this.calendarData.forEach((boothData) => {
      boothData.cells.forEach(cell => {
        cell.isInCart = this.isDateInCart(cell.boothId, cell.date);
      });
    });
  }

  /**
   * Update selected bookings indicators for all calendar cells
   */
  private updateSelectedBookingsIndicators(): void {
    this.calendarData.forEach((boothData) => {
      boothData.cells.forEach(cell => {
        cell.isInSelectedBookings = this.isDateInSelectedBookings(cell.boothId, cell.date);
      });
    });
  }

  /**
   * Calculate price using greedy algorithm with multi-period pricing and return breakdown
   * Example: 10 days with [1day=1zł, 3days=2zł, 7days=6zł]
   * Result: 1×7days + 1×3days = 1×6zł + 1×2zł = 8zł (not 10×1zł = 10zł)
   * Returns: { totalPrice: 8, breakdown: [{ daysCount: 7, price: 6, label: "7 dni (6,00 zł)" }, ...] }
   */
  private calculatePriceWithGreedyAlgorithm(booth: BoothDto, totalDays: number): { totalPrice: number; breakdown: PriceBreakdownItem[] } {
    const breakdown: PriceBreakdownItem[] = [];

    if (!booth.pricingPeriods || booth.pricingPeriods.length === 0) {
      // Fallback to legacy pricePerDay if no pricing periods
      const totalPrice = totalDays * booth.pricePerDay;
      const label = totalDays === 1
        ? `1 dzień (${booth.pricePerDay.toFixed(2)} zł)`
        : `${totalDays} dni (${totalPrice.toFixed(2)} zł)`;
      breakdown.push({
        daysCount: totalDays,
        price: totalPrice,
        label
      });
      return { totalPrice, breakdown };
    }

    // Sort periods by days descending (greedy: use largest first)
    const sortedPeriods = [...booth.pricingPeriods].sort((a, b) => b.days - a.days);

    let remainingDays = totalDays;
    let totalPrice = 0;

    // Greedy algorithm: use largest periods first
    for (const period of sortedPeriods) {
      const count = Math.floor(remainingDays / period.days);
      if (count > 0) {
        const periodTotalPrice = count * period.pricePerPeriod;
        totalPrice += periodTotalPrice;
        remainingDays -= count * period.days;

        // Build label
        let label: string;
        if (count === 1) {
          label = `${period.days} dni (${period.pricePerPeriod.toFixed(2)} zł)`;
        } else {
          label = `${count} x ${period.days} dni (${periodTotalPrice.toFixed(2)} zł)`;
        }

        breakdown.push({
          daysCount: count * period.days,
          price: periodTotalPrice,
          label
        });
      }

      if (remainingDays === 0) {
        break;
      }
    }

    // If there are remaining days not covered by any period, use smallest period's daily rate
    if (remainingDays > 0) {
      const smallestPeriod = sortedPeriods[sortedPeriods.length - 1];
      const pricePerDay = smallestPeriod.pricePerPeriod / smallestPeriod.days;
      const remainingPrice = remainingDays * pricePerDay;
      totalPrice += remainingPrice;

      // Build label for remaining days
      let label: string;
      if (remainingDays === 1) {
        label = `1 dzień (${pricePerDay.toFixed(2)} zł)`;
      } else {
        label = `${remainingDays} x 1 dzień (${remainingPrice.toFixed(2)} zł)`;
      }

      breakdown.push({
        daysCount: remainingDays,
        price: remainingPrice,
        label
      });
    }

    return { totalPrice, breakdown };
  }

  /**
   * Validate gaps between selected dates and existing rentals
   */
  private validateGaps(boothData: BoothCalendarData, startDate: Date, endDate: Date): string[] {
    const errors: string[] = [];

    if (!boothData.responseData) {
      return errors;
    }

    const calendarDates = boothData.responseData.dates;
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
      if (lastRentalEndDate >= today) {
        const gapDays = Math.round((startDate.getTime() - lastRentalEndDate.getTime()) / (1000 * 60 * 60 * 24)) - 1;

        if (gapDays > 0 && gapDays < this.minimumGapDays) {
          errors.push(`Zbyt mała przerwa przed rezerwacją. Poprzednia rezerwacja kończy się ${this.formatDate(lastRentalEndDate)}, wymagana minimalnie przerwa: ${this.minimumGapDays} dni`);
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
        errors.push(`Zbyt mała przerwa po rezerwacji. Następna rezerwacja zaczyna się ${this.formatDate(nextRentalStartDate)}, wymagana minimalnie przerwa: ${this.minimumGapDays} dni`);
      }
    }

    return errors;
  }

  getCellClass(cell: CalendarGridCell): string {
    // Highlight range selection with highest priority
    if (cell.isRangeStart || cell.isRangeEnd) {
      return 'cell-range-end';
    }
    if (cell.isInRange) {
      return 'cell-in-range';
    }

    // Check if date is in selected bookings (high priority - block selection)
    if (cell.isInSelectedBookings) {
      return 'cell-in-selected-bookings';
    }

    // Check if date is in cart (before availability check)
    if (cell.isInCart) {
      return 'cell-in-cart';
    }

    // Past dates without active reservations
    if (cell.isPastDate) {
      return 'cell-past';
    }

    // Status-based styling
    let statusClass = '';
    if (cell.status === CalendarDateStatus.Reserved) {
      statusClass = 'cell-reserved';
    } else if (cell.status === CalendarDateStatus.Occupied) {
      statusClass = 'cell-occupied';
    } else if (cell.isAvailable) {
      statusClass = 'cell-available';
    }

    // Check if date is in the past - if so, add past-variant class
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (cell.date < today && statusClass) {
      return `${statusClass} cell-past-variant`;
    }

    return statusClass || 'cell-available';
  }

  getDayHeaderClass(day: Date): string {
    const today = startOfDay(new Date());
    if (isSameDay(day, today)) {
      return 'day-header-today';
    }
    return '';
  }

  canAddToSelectedBookings(): boolean {
    return this.currentSelectionBoothId !== undefined &&
           this.validationErrors.length === 0 &&
           (this.boothTypes.length === 0 || this.selectedBoothTypeId !== undefined); // Require booth type if available
  }

  getSelectedBoothData(): BoothCalendarData | undefined {
    if (!this.currentSelectionBoothId) {
      return undefined;
    }
    return this.calendarData.get(this.currentSelectionBoothId);
  }

  getSelectedBoothType(): BoothTypeDto | undefined {
    if (!this.selectedBoothTypeId) {
      return undefined;
    }
    return this.boothTypes.find(type => type.id === this.selectedBoothTypeId);
  }

  getBoothTypeById(boothTypeId: string): BoothTypeDto | undefined {
    return this.boothTypes.find(type => type.id === boothTypeId);
  }

  getPriceBreakdownForSelection(): PriceBreakdownItem[] {
    const boothData = this.getSelectedBoothData();
    if (!boothData || !boothData.selectedStartDate || !boothData.selectedEndDate || !boothData.boothDto) {
      return [];
    }

    const daysCount = this.calculateDaysCount(boothData.selectedStartDate, boothData.selectedEndDate);
    const priceResult = this.calculatePriceWithGreedyAlgorithm(boothData.boothDto, daysCount);
    return priceResult.breakdown;
  }

  getTotalPriceForSelection(): number {
    const boothData = this.getSelectedBoothData();
    if (!boothData || !boothData.selectedStartDate || !boothData.selectedEndDate || !boothData.boothDto) {
      return 0;
    }

    const daysCount = this.calculateDaysCount(boothData.selectedStartDate, boothData.selectedEndDate);
    const priceResult = this.calculatePriceWithGreedyAlgorithm(boothData.boothDto, daysCount);
    return priceResult.totalPrice;
  }

  getMonthHeaders(): { monthName: string; daysInMonth: number }[] {
    const monthHeaders: { monthName: string; daysInMonth: number; month: number; year: number }[] = [];
    const processedMonths = new Set<string>();

    for (const day of this.allCalendarDays) {
      const month = day.getMonth();
      const year = day.getFullYear();
      const monthKey = `${year}-${month}`;

      // Check if this is a new month (using unique key)
      if (!processedMonths.has(monthKey)) {
        processedMonths.add(monthKey);

        // Count days in this month within our date range
        const monthStart = startOfMonth(day);
        const monthEnd = endOfMonth(day);
        const daysInThisMonth = this.allCalendarDays.filter(d =>
          d >= monthStart && d <= monthEnd
        ).length;

        const monthName = format(day, 'MMMM yyyy', { locale: pl });

        monthHeaders.push({
          monthName: monthName.charAt(0).toUpperCase() + monthName.slice(1),
          daysInMonth: daysInThisMonth,
          month: month,
          year: year
        });
      }
    }

    return monthHeaders;
  }

  /**
   * Load all floor plans containing the selected booth
   */
  private loadFloorPlansForBooth(boothId: string): void {
    this.floorPlanService.getListByTenant(undefined, true)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (floorPlans) => {
          // Filter floor plans that contain this booth
          this.selectedBoothFloorPlans = floorPlans.filter(plan =>
            plan.booths.some(booth => booth.boothId === boothId)
          );

          if (this.selectedBoothFloorPlans.length > 0) {
            this.showFloorPlanSection = true;
            // Initialize canvases after DOM update
            setTimeout(() => {
              this.initializeFloorPlanCanvases();
            });
          } else {
            this.showFloorPlanSection = false;
          }
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Error loading floor plans for booth:', error);
          this.selectedBoothFloorPlans = [];
          this.showFloorPlanSection = false;
          this.cdr.markForCheck();
        }
      });
  }

  /**
   * Initialize all floor plan canvases
   */
  private initializeFloorPlanCanvases(): void {
    if (!this.floorPlanContainer) {
      console.warn('Floor plan container not found');
      return;
    }

    this.selectedBoothFloorPlans.forEach(floorPlan => {
      const canvasId = `floor-plan-canvas-${floorPlan.id}`;
      const canvasElement = this.floorPlanContainer?.nativeElement.querySelector(`#${canvasId}`) as HTMLCanvasElement;

      if (canvasElement) {
        this.initializeFloorPlanCanvas(floorPlan, canvasElement);
      }
    });
  }

  /**
   * Initialize a single floor plan canvas
   */
  private initializeFloorPlanCanvas(floorPlan: FloorPlanDto, canvasElement: HTMLCanvasElement): void {
    try {
      // Create Fabric canvas
      const canvas = new Canvas(canvasElement, {
        width: 800,
        height: 600,
        backgroundColor: '#ffffff',
        selection: false,
        interactive: false
      });

      // Render floor plan elements and booths with opacity for non-selected elements
      const elementsOpacity = 0.35; // Gray out elements when booth is selected
      this.renderFloorPlanElements(canvas, floorPlan, elementsOpacity);
      this.renderFloorPlanBooths(canvas, floorPlan);

      // Store canvas reference
      this.floorPlanCanvases.set(floorPlan.id, canvas);
    } catch (error) {
      console.error(`Error initializing canvas for floor plan ${floorPlan.id}:`, error);
    }
  }

  /**
   * Render floor plan elements (walls, doors, etc.)
   */
  private renderFloorPlanElements(canvas: Canvas, floorPlan: FloorPlanDto, elementsOpacity: number = 0.35): void {
    if (!floorPlan.elements || floorPlan.elements.length === 0) {
      return;
    }

    floorPlan.elements.forEach(element => {
      const rect = new Rect({
        left: element.x,
        top: element.y,
        width: element.width,
        height: element.height,
        fill: element.color || '#ccc',
        stroke: '#333',
        strokeWidth: 1,
        angle: element.rotation,
        opacity: elementsOpacity,
        selectable: false,
        evented: false
      });

      canvas.add(rect);
    });

    canvas.renderAll();
  }

  /**
   * Render floor plan booths with highlighting for selected booth
   */
  private renderFloorPlanBooths(canvas: Canvas, floorPlan: FloorPlanDto): void {
    if (!floorPlan.booths || floorPlan.booths.length === 0) {
      return;
    }

    floorPlan.booths.forEach(boothData => {
      const isSelectedBooth = boothData.boothId === this.currentSelectionBoothId;

      // Determine color and styling
      const fillColor = isSelectedBooth ? '#4A90E2' : '#4CAF50'; // Blue for selected, green for others
      const opacity = isSelectedBooth ? 1 : 0.35;
      const strokeWidth = isSelectedBooth ? 3 : 2;
      const strokeColor = isSelectedBooth ? '#2E5AA8' : '#333';

      // Create booth rectangle
      const rect = new Rect({
        left: 0,
        top: 0,
        width: boothData.width,
        height: boothData.height,
        fill: fillColor,
        stroke: strokeColor,
        strokeWidth: strokeWidth,
        rx: 4,
        ry: 4,
        selectable: false,
        evented: false,
        opacity: opacity
      });

      // Create booth number text
      const numberFontSize = Math.min(boothData.width / 3.5, 18);
      const boothNumber = new Text(boothData.booth?.number || '', {
        left: boothData.width / 2,
        top: boothData.height / 2,
        fontSize: numberFontSize,
        fontFamily: 'Arial, sans-serif',
        fill: '#fff',
        textAlign: 'center',
        originX: 'center',
        originY: 'center',
        selectable: false,
        evented: false,
        fontWeight: 'bold',
        opacity: opacity
      });

      const elements: any[] = [rect, boothNumber];

      // Add pulsating animation for selected booth
      if (isSelectedBooth) {
        // Add shadow with animation class
        rect.set({
          shadow: new Shadow({
            color: 'rgba(74, 144, 226, 0.5)',
            blur: 15,
            offsetX: 0,
            offsetY: 0
          })
        });
      }

      // Create group with all elements
      const group = new Group(elements, {
        left: boothData.x,
        top: boothData.y,
        angle: boothData.rotation,
        selectable: false,
        evented: false
      });

      (group as any).boothData = boothData;
      canvas.add(group);
    });

    canvas.renderAll();
  }

  /**
   * Clear all floor plan canvases and reset state
   */
  private clearFloorPlans(): void {
    // Dispose all canvas instances
    this.floorPlanCanvases.forEach((canvas) => {
      if (canvas) {
        try {
          canvas.dispose();
        } catch (error) {
          console.error('Error disposing canvas:', error);
        }
      }
    });

    this.floorPlanCanvases.clear();
    this.selectedBoothFloorPlans = [];
    this.showFloorPlanSection = false;
    this.cdr.markForCheck();
  }

  /**
   * Track by floor plan ID for ngFor optimization
   */
  trackByFloorPlanId(index: number, floorPlan: FloorPlanDto): string {
    return floorPlan.id;
  }

  trackByBoothId(index: number, item: any): string {
    if (item.boothId) {
      return item.boothId;
    }
    return index.toString();
  }

  trackByBookingIndex(index: number): number {
    return index;
  }

  trackByDayIndex(index: number): number {
    return index;
  }

  trackByMonthIndex(index: number, item: { monthName: string; daysInMonth: number }): string {
    return item.monthName;
  }

  trackByIndex(index: number): number {
    return index;
  }
}
