import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface AddItemToSheetDto {
  itemId: string;
  commissionPercentage: number;
}

export interface AssignSheetToRentalDto {
  rentalId: string;
}

export interface CreateItemDto {
  name: string;
  category?: string;
  price: number;
  currency: string;
}

export interface CreateItemSheetDto {
}

export interface ItemDto extends FullAuditedEntityDto<string> {
  userId?: string;
  name?: string;
  category?: string;
  price: number;
  currency?: string;
  status?: string;
}

export interface ItemSheetDto extends FullAuditedEntityDto<string> {
  userId?: string;
  rentalId?: string;
  boothNumber?: string;
  status?: string;
  items: ItemSheetItemDto[];
  totalItemsCount: number;
  soldItemsCount: number;
  reclaimedItemsCount: number;
}

export interface ItemSheetItemDto extends FullAuditedEntityDto<string> {
  itemSheetId?: string;
  itemId?: string;
  itemNumber: number;
  barcode?: string;
  commissionPercentage: number;
  status?: string;
  soldAt?: string;
  item: ItemDto;
}

export interface UpdateItemDto {
  name: string;
  category?: string;
  price: number;
  currency: string;
}
