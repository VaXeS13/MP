import type { FullAuditedEntityDto } from '@abp/ng.core';

export interface AddItemToSheetDto {
  itemId: string;
  commissionPercentage: number;
}

export interface AssignSheetToRentalDto {
  rentalId: string;
}

export interface BatchAddItemsDto {
  sheetId: string;
  itemIds: string[];
  commissionPercentage: number;
}

export interface BatchAddItemsResultDto {
  results: BatchItemResultDto[];
  successCount: number;
  failureCount: number;
}

export interface BatchItemResultDto {
  itemId?: string;
  success: boolean;
  errorMessage?: string;
}

export interface BulkItemCreationResultDto {
  successCount: number;
  failureCount: number;
  createdItems: ItemDto[];
  errors: BulkItemErrorDto[];
}

export interface BulkItemEntryDto {
  name: string;
  category?: string;
  price: number;
}

export interface BulkItemErrorDto {
  itemIndex: number;
  itemName?: string;
  errorMessage?: string;
}

export interface CreateBulkItemsDto {
  items: BulkItemEntryDto[];
}

export interface CreateItemDto {
  name: string;
  category?: string;
  price: number;
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
}
