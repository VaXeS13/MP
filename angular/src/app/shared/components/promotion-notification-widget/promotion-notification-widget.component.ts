import { Component, OnInit, OnDestroy } from '@angular/core';
import { PromotionService } from '../../../proxy/promotions/promotion.service';
import { PromotionDto } from '../../../proxy/promotions/models';
import { PromotionDisplayMode } from '../../../proxy/promotions/promotion-display-mode.enum';
import { PromotionDismissalService } from '../../services/promotion-dismissal.service';
import { Subject, takeUntil, timer } from 'rxjs';

@Component({
  selector: 'app-promotion-notification-widget',
  standalone: false,
  templateUrl: './promotion-notification-widget.component.html',
  styleUrls: ['./promotion-notification-widget.component.scss']
})
export class PromotionNotificationWidgetComponent implements OnInit, OnDestroy {
  promotions: PromotionDto[] = [];
  currentPromotionIndex = 0;
  visible = false;
  loading = false;

  private destroy$ = new Subject<void>();
  private rotationInterval = 5000; // Rotate promotions every 5 seconds

  PromotionDisplayMode = PromotionDisplayMode;

  constructor(
    private promotionService: PromotionService,
    private promotionDismissalService: PromotionDismissalService
  ) {}

  ngOnInit(): void {
    this.loadActivePromotions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadActivePromotions(): void {
    this.loading = true;
    this.promotionService.getActivePromotions().subscribe({
      next: (promotions) => {
        // Filter promotions with visible display modes and not dismissed
        this.promotions = promotions.filter(
          p => p.displayMode !== PromotionDisplayMode.None &&
               !this.promotionDismissalService.isPromotionDismissed(p.id)
        );

        if (this.promotions.length > 0) {
          this.visible = true;
          this.startRotation();
        }

        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  startRotation(): void {
    if (this.promotions.length <= 1) return;

    timer(this.rotationInterval, this.rotationInterval)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPromotionIndex = (this.currentPromotionIndex + 1) % this.promotions.length;
      });
  }

  get currentPromotion(): PromotionDto | null {
    return this.promotions[this.currentPromotionIndex] || null;
  }

  get displayPosition(): string {
    const promotion = this.currentPromotion;
    if (!promotion) return 'bottom-right';

    switch (promotion.displayMode) {
      case PromotionDisplayMode.StickyBottomRight:
        return 'bottom-right';
      case PromotionDisplayMode.StickyBottomLeft:
        return 'bottom-left';
      default:
        return 'bottom-right';
    }
  }

  getPromotionMessage(promotion: PromotionDto): string {
    return promotion.customerMessage || '';
  }

  close(): void {
    const currentPromotion = this.currentPromotion;
    if (currentPromotion) {
      // Mark promotion as dismissed for 1 hour
      this.promotionDismissalService.dismissPromotion(currentPromotion.id);

      // Remove the dismissed promotion from the list
      this.promotions = this.promotions.filter(p => p.id !== currentPromotion.id);

      // Adjust current index if needed
      if (this.currentPromotionIndex >= this.promotions.length) {
        this.currentPromotionIndex = Math.max(0, this.promotions.length - 1);
      }

      // Hide widget if no promotions left
      if (this.promotions.length === 0) {
        this.visible = false;
      }
    } else {
      this.visible = false;
    }
  }

  nextPromotion(): void {
    if (this.promotions.length <= 1) return;
    this.currentPromotionIndex = (this.currentPromotionIndex + 1) % this.promotions.length;
  }

  previousPromotion(): void {
    if (this.promotions.length <= 1) return;
    this.currentPromotionIndex =
      (this.currentPromotionIndex - 1 + this.promotions.length) % this.promotions.length;
  }
}
