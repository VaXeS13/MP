import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CartService } from '../../services/cart.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { CartDto, CartItemDto, UpdateCartItemDto } from '../../shared/models/cart.model';
import { MessageService, ConfirmationService } from 'primeng/api';
import { BoothTypeService } from '../../services/booth-type.service';
import { BoothTypeDto } from '../../shared/models/booth-type.model';
import { RentalService } from '../../services/rental.service';
import { BoothSettingsService } from '../../services/booth-settings.service';
import { BoothCalendarRequestDto, CalendarDateDto, CalendarDateStatus } from '../../shared/models/rental.model';
import { forkJoin, interval, Subscription } from 'rxjs';
import { LocalizationService } from '@abp/ng.core';
import { PromotionService } from '../../proxy/promotions/promotion.service';

@Component({
  selector: 'app-cart',
  standalone: false,
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss'
})
export class CartComponent implements OnInit, OnDestroy {
  cart: CartDto | null = null;
  loading = false;
  showEditDialog = false;
  editingItem: CartItemDto | null = null;
  tenantCurrencyCode: string = 'PLN';

  // Validation
  itemValidationErrors = new Map<string, string>(); // cartItemId -> error message
  minimumGapDays = 7;
  validating = false;

  // Reservation countdown
  private countdownSubscription?: Subscription;
  itemCountdowns = new Map<string, string>(); // cartItemId -> countdown display string

  // Promo code
  promoCode: string = '';
  applyingPromoCode = false;
  promoCodeError: string = '';
  promoCodeSuccess: string = '';
  showBubbleAnimation = false;

  constructor(
    public cartService: CartService,
    private router: Router,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private rentalService: RentalService,
    private boothSettingsService: BoothSettingsService,
    private tenantCurrencyService: TenantCurrencyService,
    private localization: LocalizationService,
    private promotionService: PromotionService
  ) {}

  ngOnInit(): void {
    this.loadBoothSettings();

    // Load tenant currency
    this.tenantCurrencyService.getCurrency().subscribe(result => {
      this.tenantCurrencyCode = this.tenantCurrencyService.getCurrencyName(result.currency);
    });

    this.loadCart();
    this.startCountdownTimer();
  }

  ngOnDestroy(): void {
    this.stopCountdownTimer();
  }

  private startCountdownTimer(): void {
    // Update countdown every second
    this.countdownSubscription = interval(1000).subscribe(() => {
      this.updateCountdowns();
    });
  }

  private stopCountdownTimer(): void {
    if (this.countdownSubscription) {
      this.countdownSubscription.unsubscribe();
    }
  }

  private updateCountdowns(): void {
    if (!this.cart || !this.cart.items) return;

    const now = new Date();

    this.cart.items.forEach(item => {
      if (!item.reservationExpiresAt) {
        this.itemCountdowns.delete(item.id);
        return;
      }

      const expiresAt = new Date(item.reservationExpiresAt);
      const diff = expiresAt.getTime() - now.getTime();

      if (diff <= 0) {
        // Reservation expired - show 00:00 but keep item in cart
        // Background worker will release reservation, but CartItem stays
        this.itemCountdowns.set(item.id, '00:00');
      } else {
        // Calculate remaining time
        const minutes = Math.floor(diff / 60000);
        const seconds = Math.floor((diff % 60000) / 1000);
        this.itemCountdowns.set(item.id, `${minutes}:${seconds.toString().padStart(2, '0')}`);
      }
    });

    // NOTE: We do NOT reload cart when items expire
    // Items stay in cart, only reservation is released by background worker
  }

  getCountdown(itemId: string): string {
    return this.itemCountdowns.get(itemId) || '';
  }

  hasReservation(item: CartItemDto): boolean {
    return !!item.reservationExpiresAt;
  }

  isReservationExpiring(item: CartItemDto): boolean {
    if (!item.reservationExpiresAt) return false;
    const expiresAt = new Date(item.reservationExpiresAt);
    const now = new Date();
    const diff = expiresAt.getTime() - now.getTime();
    return diff > 0 && diff <= 60000; // Less than 1 minute remaining
  }

  loadBoothSettings(): void {
    this.boothSettingsService.get().subscribe({
      next: (settings) => {
        this.minimumGapDays = settings.minimumGapDays;
      },
      error: (error) => {
        console.error('Error loading booth settings:', error);
      }
    });
  }

  loadCart(): void {
    this.loading = true;
    this.cartService.getMyCart().subscribe({
      next: (cart) => {
        this.cart = cart;
        this.loading = false;
        // Validate all items after loading
        this.validateAllItems();
      },
      error: (error) => {
        console.error('Error loading cart:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: this.localization.instant('::Cart:LoadError')
        });
        this.loading = false;
      }
    });
  }

  validateAllItems(): void {
    if (!this.cart || this.cart.itemCount === 0) {
      this.itemValidationErrors.clear();
      this.validating = false;
      return;
    }

    this.validating = true;
    this.itemValidationErrors.clear();

    // Create calendar requests for all items
    const calendarRequests = this.cart.items.map(item => {
      const startDate = new Date(item.startDate);
      const endDate = new Date(item.endDate);

      // Load calendar for 2 months around the rental period
      const calendarStart = new Date(startDate);
      calendarStart.setMonth(calendarStart.getMonth() - 1);
      const calendarEnd = new Date(endDate);
      calendarEnd.setMonth(calendarEnd.getMonth() + 1);

      const request: BoothCalendarRequestDto = {
        boothId: item.boothId,
        startDate: this.formatDateForApi(calendarStart),
        endDate: this.formatDateForApi(calendarEnd),
        excludeCartId: this.cart!.id
      };

      return this.rentalService.getBoothCalendar(request);
    });

    // Check if there are any requests before calling forkJoin
    if (calendarRequests.length === 0) {
      this.validating = false;
      return;
    }

    // Execute all calendar requests in parallel
    forkJoin(calendarRequests).subscribe({
      next: (responses) => {
        // Validate each item with its calendar data
        this.cart!.items.forEach((item, index) => {
          const calendarData = responses[index];
          const error = this.validateItem(item, calendarData.dates);
          if (error) {
            this.itemValidationErrors.set(item.id, error);
          }
        });
        this.validating = false;
      },
      error: (error) => {
        console.error('Error validating cart items:', error);
        this.validating = false;
      }
    });
  }

  validateItem(item: CartItemDto, calendarDates: CalendarDateDto[]): string | null {
    const startDate = new Date(item.startDate);
    const endDate = new Date(item.endDate);

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
        return `${gapDays}-day gap before rental (minimum ${this.minimumGapDays} days required)`;
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
        return `${gapDays}-day gap after rental (minimum ${this.minimumGapDays} days required)`;
      }
    }

    return null;
  }

  private formatDateForApi(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  removeItem(item: CartItemDto): void {
    this.confirmationService.confirm({
      message: `${this.localization.instant('::Common:Remove')} ${item.boothNumber} ${this.localization.instant('::Cart:FromCart')}?`,
      header: this.localization.instant('::Cart:ConfirmRemove'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.cartService.removeItem(item.id).subscribe({
          next: (cart) => {
            this.cart = cart;
            this.messageService.add({
              severity: 'success',
              summary: this.localization.instant('::Messages:Removed'),
              detail: this.localization.instant('::Cart:ItemRemoved')
            });
            // Revalidate after removal
            this.validateAllItems();
          },
          error: (error) => {
            console.error('Error removing item:', error);
            this.messageService.add({
              severity: 'error',
              summary: this.localization.instant('::Messages:Error'),
              detail: this.localization.instant('::Cart:RemoveError')
            });
          }
        });
      }
    });
  }

  clearCart(): void {
    this.confirmationService.confirm({
      message: this.localization.instant('::Cart:ClearConfirm'),
      header: this.localization.instant('::Cart:ConfirmClear'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.cartService.clearCart().subscribe({
          next: (cart) => {
            this.cart = cart;
            this.messageService.add({
              severity: 'success',
              summary: this.localization.instant('::Messages:Cleared'),
              detail: this.localization.instant('::Cart:ClearSuccess')
            });
            // Clear validation errors
            this.itemValidationErrors.clear();
          },
          error: (error) => {
            console.error('Error clearing cart:', error);
            this.messageService.add({
              severity: 'error',
              summary: this.localization.instant('::Messages:Error'),
              detail: this.localization.instant('::Cart:ClearError')
            });
          }
        });
      }
    });
  }

  proceedToCheckout(): void {
    if (!this.cart || this.cart.itemCount === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: this.localization.instant('::Cart:EmptyCart'),
        detail: this.localization.instant('::Cart:AddItemsFirst')
      });
      return;
    }

    // Check for validation errors
    if (this.hasValidationErrors) {
      this.messageService.add({
        severity: 'error',
        summary: this.localization.instant('::Cart:InvalidItems'),
        detail: this.localization.instant('::Cart:PleaseFixIssues'),
        life: 10000
      });
      return;
    }

    this.router.navigate(['/cart/checkout']);
  }

  get hasValidationErrors(): boolean {
    return this.itemValidationErrors.size > 0;
  }

  get hasExpiredItems(): boolean {
    return !!this.cart && this.cart.items.some(item => item.isExpired);
  }

  get expiredItemsCount(): number {
    return this.cart?.items.filter(item => item.isExpired).length || 0;
  }

  get canProceedToCheckout(): boolean {
    return !this.hasValidationErrors && !this.validating && !!this.cart && this.cart.itemCount > 0;
  }

  getValidationErrorsTooltip(): string {
    if (!this.cart || this.itemValidationErrors.size === 0) {
      return '';
    }

    const errorItems: string[] = [];
    this.cart.items.forEach(item => {
      const error = this.itemValidationErrors.get(item.id);
      if (error) {
        errorItems.push(`<li>${this.localization.instant('::Common:Booth')} ${item.boothNumber}: ${error}</li>`);
      }
    });

    return `<strong>${this.localization.instant('::Cart:PleaseFixIssues')}:</strong><ul style="margin-top: 8px; padding-left: 20px;">${errorItems.join('')}</ul>`;
  }

  continueShopping(): void {
    this.router.navigate(['/floor-plans']);
  }

  openEditDialog(item: CartItemDto): void {
    this.editingItem = item;
    this.showEditDialog = true;
  }

  onEditSaved(data: { boothTypeId: string; startDate: string; endDate: string; notes?: string }): void {
    if (!this.editingItem) return;

    const updateDto: UpdateCartItemDto = {
      boothTypeId: data.boothTypeId,
      startDate: data.startDate,
      endDate: data.endDate,
      notes: data.notes || '' // Use notes from dialog
    };

    this.cartService.updateItem(this.editingItem.id, updateDto).subscribe({
      next: (cart) => {
        this.cart = cart;
        this.showEditDialog = false;
        this.editingItem = null;
        this.messageService.add({
          severity: 'success',
          summary: this.localization.instant('::Messages:Updated'),
          detail: this.localization.instant('::Cart:ItemUpdateSuccess')
        });
        // Revalidate after update
        this.validateAllItems();
      },
      error: (error) => {
        console.error('Error updating item:', error);

        const errorCode = error.error?.error?.code;
        const errorData = error.error?.error?.data;

        if (errorCode === 'RENTAL_CREATES_UNUSABLE_GAP_BEFORE') {
          const gapDays = errorData?.GapDays || 0;
          const minimumGapDays = errorData?.MinimumGapDays || 0;
          const suggestedDate = errorData?.SuggestedStartDate ? new Date(errorData.SuggestedStartDate).toLocaleDateString() : '';
          const alternativeDate = errorData?.AlternativeStartDate ? new Date(errorData.AlternativeStartDate).toLocaleDateString() : '';

          const message = `Your rental would leave a ${gapDays}-day gap before another rental. This gap is too small to be rented out (minimum ${minimumGapDays} days required). Please adjust your start date to ${suggestedDate} or ${alternativeDate}.`;
          this.messageService.add({
            severity: 'error',
            summary: this.localization.instant('::Cart:GapBeforeTitle'),
            detail: message,
            life: 10000
          });
        } else if (errorCode === 'RENTAL_CREATES_UNUSABLE_GAP_AFTER') {
          const gapDays = errorData?.GapDays || 0;
          const minimumGapDays = errorData?.MinimumGapDays || 0;
          const suggestedDate = errorData?.SuggestedEndDate ? new Date(errorData.SuggestedEndDate).toLocaleDateString() : '';
          const alternativeDate = errorData?.AlternativeEndDate ? new Date(errorData.AlternativeEndDate).toLocaleDateString() : '';

          const message = `Your rental would leave a ${gapDays}-day gap after another rental. This gap is too small to be rented out (minimum ${minimumGapDays} days required). Please adjust your end date to ${suggestedDate} or ${alternativeDate}.`;
          this.messageService.add({
            severity: 'error',
            summary: this.localization.instant('::Cart:GapAfterTitle'),
            detail: message,
            life: 10000
          });
        } else {
          this.messageService.add({
            severity: 'error',
            summary: this.localization.instant('::Messages:Error'),
            detail: error.error?.error?.message || this.localization.instant('::Cart:ItemUpdateError')
          });
        }

        this.showEditDialog = false;
        this.editingItem = null;
      }
    });
  }

  hasItemError(itemId: string): boolean {
    return this.itemValidationErrors.has(itemId);
  }

  getItemError(itemId: string): string {
    return this.itemValidationErrors.get(itemId) || '';
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString();
  }

  getDuration(item: CartItemDto): string {
    const start = new Date(item.startDate);
    const end = new Date(item.endDate);
    const days = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1;
    return `${days} day${days !== 1 ? 's' : ''}`;
  }

  trackByCartItemId(index: number, item: CartItemDto): string {
    return item.id;
  }

  applyPromoCode(): void {
    if (!this.promoCode || !this.promoCode.trim()) {
      this.promoCodeError = this.localization.instant('MP::PromoCodeRequired');
      return;
    }

    this.applyingPromoCode = true;
    this.promoCodeError = '';
    this.promoCodeSuccess = '';

    this.promotionService.applyPromotionToCart({ promoCode: this.promoCode.trim() }).subscribe({
      next: () => {
        this.promoCodeSuccess = this.localization.instant('MP::PromoCodeAppliedSuccess');
        this.messageService.add({
          severity: 'success',
          summary: this.localization.instant('::Messages:Success'),
          detail: this.localization.instant('MP::PromoCodeAppliedSuccess')
        });

        // Get updated cart data without loading indicator
        this.cartService.getMyCart().subscribe({
          next: (updatedCart) => {
            this.cart = updatedCart;
            // Trigger neon border animation after price update
            this.triggerNeonBorder();
          }
        });

        this.applyingPromoCode = false;
      },
      error: (error) => {
        console.error('Error applying promo code:', error);
        const errorMessage = error.error?.error?.message || this.localization.instant('MP::PromoCodeInvalid');
        this.promoCodeError = errorMessage;
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: errorMessage
        });
        this.applyingPromoCode = false;
      }
    });
  }

  removePromoCode(): void {
    this.promotionService.removePromotionFromCart().subscribe({
      next: () => {
        this.promoCode = '';
        this.promoCodeError = '';
        this.promoCodeSuccess = '';
        this.messageService.add({
          severity: 'success',
          summary: this.localization.instant('::Messages:Removed'),
          detail: this.localization.instant('MP::PromoCodeRemovedSuccess')
        });

        // Get updated cart data without loading indicator
        this.cartService.getMyCart().subscribe({
          next: (updatedCart) => {
            this.cart = updatedCart;
            // Trigger neon border animation after price update
            this.triggerNeonBorder();
          }
        });
      },
      error: (error) => {
        console.error('Error removing promo code:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: this.localization.instant('MP::PromoCodeRemoveError')
        });
      }
    });
  }

  hasPromoCode(): boolean {
    return !!this.cart?.appliedPromotionId;
  }

  getDiscountAmount(): number {
    return this.cart?.discountAmount || 0;
  }

  getFinalAmount(): number {
    // Use the FinalAmount from cart (sum of item final prices)
    return this.cart?.finalAmount || (this.cart?.totalAmount || 0);
  }

  hasItemDiscount(item: CartItemDto): boolean {
    return item.discountAmount > 0;
  }

  getApplicableItemsCount(): number {
    return this.cart?.items?.filter(item => this.hasItemDiscount(item)).length || 0;
  }

  triggerNeonBorder(): void {
    // Show neon border animation
    this.showBubbleAnimation = true;

    // Hide animation after completion
    setTimeout(() => {
      this.showBubbleAnimation = false;
    }, 1500);
  }
}
