import { mapEnumToOptions } from '@abp/ng.core';

export enum HomePageSectionType {
  HeroBanner = 1,
  PromotionCards = 2,
  Announcement = 3,
  FeatureHighlights = 4,
  CustomHtml = 5,
  ImageGallery = 6,
}

export const homePageSectionTypeOptions = mapEnumToOptions(HomePageSectionType);
