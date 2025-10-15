import type { HomePageSectionType } from '../../../home-page-content/home-page-section-type.enum';
import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface CreateHomePageSectionDto {
  sectionType: HomePageSectionType;
  title: string;
  subtitle?: string;
  content?: string;
  imageFileId?: string;
  linkUrl?: string;
  linkText?: string;
  validFrom?: string;
  validTo?: string;
  backgroundColor?: string;
  textColor?: string;
}

export interface HomePageSectionDto extends FullAuditedEntityDto<string> {
  sectionType?: HomePageSectionType;
  title?: string;
  subtitle?: string;
  content?: string;
  imageFileId?: string;
  linkUrl?: string;
  linkText?: string;
  order: number;
  isActive: boolean;
  validFrom?: string;
  validTo?: string;
  backgroundColor?: string;
  textColor?: string;
  isValidForDisplay: boolean;
}

export interface ReorderSectionDto {
  id: string;
  order: number;
}

export interface UpdateHomePageSectionDto {
  sectionType: HomePageSectionType;
  title: string;
  subtitle?: string;
  content?: string;
  imageFileId?: string;
  linkUrl?: string;
  linkText?: string;
  validFrom?: string;
  validTo?: string;
  backgroundColor?: string;
  textColor?: string;
}
