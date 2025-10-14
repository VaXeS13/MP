import { Component, OnInit, OnDestroy } from '@angular/core';
import { PromotionService } from '../../../proxy/promotions/promotion.service';
import { PromotionDto } from '../../../proxy/promotions/models';
import { PromotionDisplayMode } from '../../../proxy/promotions/promotion-display-mode.enum';
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

  constructor(private promotionService: PromotionService) {}

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
        // Filter promotions with visible display modes
        this.promotions = promotions.filter(
          p => p.displayMode !== PromotionDisplayMode.None
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
    this.visible = false;
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
