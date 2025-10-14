import { mapEnumToOptions } from '@abp/ng.core';

export enum DiscountType {
  Percentage = 0,
  FixedAmount = 1,
}

export const discountTypeOptions = mapEnumToOptions(DiscountType);
