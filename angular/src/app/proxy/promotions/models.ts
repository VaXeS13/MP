import type { PromotionType } from './promotion-type.enum';
import type { PromotionDisplayMode } from './promotion-display-mode.enum';
import type { DiscountType } from './discount-type.enum';
import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface ApplyPromotionToCartInput {
  promoCode?: string;
}

export interface CalculateDiscountInput {
  promotionId: string;
  totalAmount: number;
}

export interface CalculateDiscountOutput {
  discountAmount: number;
  finalAmount: number;
  promotionName?: string;
}

export interface CreatePromotionDto {
  name: string;
  description?: string;
  type: PromotionType;
  displayMode: PromotionDisplayMode;
  validFrom?: string;
  validTo?: string;
  priority: number;
  minimumBoothsCount?: number;
  promoCode?: string;
  discountType: DiscountType;
  discountValue: number;
  maxDiscountAmount?: number;
  maxUsageCount?: number;
  maxUsagePerUser?: number;
  customerMessage?: string;
  maxAccountAgeDays?: number;
  isActive: boolean;
  applicableBoothTypeIds: string[];
  applicableBoothIds: string[];
}

export interface GetPromotionsInput extends PagedAndSortedResultRequestDto {
  filterText?: string;
  isActive?: boolean;
  type?: PromotionType;
}

export interface PromotionDto extends FullAuditedEntityDto<string> {
  name?: string;
  description?: string;
  type?: PromotionType;
  displayMode?: PromotionDisplayMode;
  isActive: boolean;
  validFrom?: string;
  validTo?: string;
  priority: number;
  minimumBoothsCount?: number;
  promoCode?: string;
  requiresPromoCode: boolean;
  discountType?: DiscountType;
  discountValue: number;
  maxDiscountAmount?: number;
  maxUsageCount?: number;
  currentUsageCount: number;
  maxUsagePerUser?: number;
  customerMessage?: string;
  maxAccountAgeDays?: number;
  applicableBoothTypeIds: string[];
  applicableBoothIds: string[];
}

export interface UpdatePromotionDto {
  name: string;
  description?: string;
  type: PromotionType;
  displayMode: PromotionDisplayMode;
  validFrom?: string;
  validTo?: string;
  priority: number;
  minimumBoothsCount?: number;
  promoCode?: string;
  discountType: DiscountType;
  discountValue: number;
  maxDiscountAmount?: number;
  maxUsageCount?: number;
  maxUsagePerUser?: number;
  customerMessage?: string;
  maxAccountAgeDays?: number;
  isActive: boolean;
  applicableBoothTypeIds: string[];
  applicableBoothIds: string[];
}

export interface ValidatePromoCodeInput {
  promoCode: string;
}
