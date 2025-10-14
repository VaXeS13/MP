import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ConfirmationService } from '@abp/ng.theme.shared';
import { PromotionService } from '../../proxy/promotions/promotion.service';
import { PromotionDto, GetPromotionsInput } from '../../proxy/promotions/models';
import { PromotionType, promotionTypeOptions } from '../../proxy/promotions/promotion-type.enum';
import { DiscountType } from '../../proxy/promotions/discount-type.enum';
import { PromotionDisplayMode } from '../../proxy/promotions/promotion-display-mode.enum';
import { PagedResultDto } from '@abp/ng.core';

@Component({
  selector: 'app-promotion-list',
  standalone: false,
  templateUrl: './promotion-list.component.html',
  styleUrls: ['./promotion-list.component.scss']
})
export class PromotionListComponent implements OnInit {
  promotions: PromotionDto[] = [];
  totalCount = 0;
  loading = false;

  filters: GetPromotionsInput = {
    filterText: '',
    isActive: undefined,
    type: undefined,
    sorting: 'priority DESC, creationTime DESC',
    skipCount: 0,
    maxResultCount: 10
  };

  promotionTypeOptions = promotionTypeOptions;
  PromotionType = PromotionType;
  DiscountType = DiscountType;
  PromotionDisplayMode = PromotionDisplayMode;

  activeFilterOptions = [
    { label: 'All', value: undefined },
    { label: 'Active', value: true },
    { label: 'Inactive', value: false }
  ];

  constructor(
    private promotionService: PromotionService,
    private router: Router,
    private confirmation: ConfirmationService
  ) {}

  ngOnInit(): void {
    this.loadPromotions();
  }

  loadPromotions(): void {
    this.loading = true;
    this.promotionService.getList(this.filters).subscribe({
      next: (result: PagedResultDto<PromotionDto>) => {
        this.promotions = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onPageChange(event: any): void {
    this.filters.skipCount = event.first;
    this.filters.maxResultCount = event.rows;
    this.loadPromotions();
  }

  onFilterChange(): void {
    this.filters.skipCount = 0;
    this.loadPromotions();
  }

  createNew(): void {
    this.router.navigate(['/promotions/new']);
  }

  edit(id: string): void {
    this.router.navigate(['/promotions', id, 'edit']);
  }

  delete(promotion: PromotionDto): void {
    this.confirmation.warn('MP::PromotionDeletionConfirmationMessage', 'MP::AreYouSure').subscribe((status) => {
      if (status === 'confirm') {
        this.promotionService.delete(promotion.id!).subscribe(() => {
          this.loadPromotions();
        });
      }
    });
  }

  activate(promotion: PromotionDto): void {
    this.promotionService.activate(promotion.id!).subscribe(() => {
      this.loadPromotions();
    });
  }

  deactivate(promotion: PromotionDto): void {
    this.promotionService.deactivate(promotion.id!).subscribe(() => {
      this.loadPromotions();
    });
  }

  getDiscountDisplay(promotion: PromotionDto): string {
    if (promotion.discountType === DiscountType.Percentage) {
      return `${promotion.discountValue}%`;
    } else {
      return `${promotion.discountValue} PLN`;
    }
  }

  getPromotionTypeLabel(type?: PromotionType): string {
    const option = this.promotionTypeOptions.find(o => o.value === type);
    return option?.key || '';
  }

  getValidityStatus(promotion: PromotionDto): string {
    const now = new Date();
    const validFrom = promotion.validFrom ? new Date(promotion.validFrom) : null;
    const validTo = promotion.validTo ? new Date(promotion.validTo) : null;

    if (validFrom && now < validFrom) {
      return 'Not Started';
    }
    if (validTo && now > validTo) {
      return 'Expired';
    }
    return 'Valid';
  }

  getUsageDisplay(promotion: PromotionDto): string {
    if (!promotion.maxUsageCount) {
      return `${promotion.currentUsageCount} / Unlimited`;
    }
    return `${promotion.currentUsageCount} / ${promotion.maxUsageCount}`;
  }
}
