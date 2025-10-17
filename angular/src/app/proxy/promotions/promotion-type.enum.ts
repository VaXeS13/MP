import { mapEnumToOptions } from '@abp/ng.core';

export enum PromotionType {
  Quantity = 0,
  PromoCode = 1,
  DateRange = 2,
  NewUser = 3,
}

export const promotionTypeOptions = mapEnumToOptions(PromotionType);
