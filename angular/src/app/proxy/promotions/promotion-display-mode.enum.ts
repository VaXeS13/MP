import { mapEnumToOptions } from '@abp/ng.core';

export enum PromotionDisplayMode {
  None = 0,
  StickyBottomRight = 1,
  StickyBottomLeft = 2,
  Popup = 3,
  Banner = 4,
}

export const promotionDisplayModeOptions = mapEnumToOptions(PromotionDisplayMode);
