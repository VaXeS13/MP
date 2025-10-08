import type { AddItemToSheetDto, AssignSheetToRentalDto, CreateItemSheetDto, ItemSheetDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ItemSheetService {
  apiName = 'Default';
  

  addItemToSheet = (sheetId: string, input: AddItemToSheetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'POST',
      url: `/api/app/item-sheet/item-to-sheet/${sheetId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  assignToRental = (sheetId: string, input: AssignSheetToRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'POST',
      url: `/api/app/item-sheet/assign-to-rental/${sheetId}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateItemSheetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'POST',
      url: '/api/app/item-sheet',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/item-sheet/${id}`,
    },
    { apiName: this.apiName,...config });
  

  findByBarcode = (barcode: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'POST',
      url: '/api/app/item-sheet/find-by-barcode',
      params: { barcode },
    },
    { apiName: this.apiName,...config });
  

  generateBarcodes = (sheetId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'POST',
      url: `/api/app/item-sheet/generate-barcodes/${sheetId}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'GET',
      url: `/api/app/item-sheet/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ItemSheetDto>>({
      method: 'GET',
      url: '/api/app/item-sheet',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMyItemSheets = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<ItemSheetDto>>({
      method: 'GET',
      url: '/api/app/item-sheet/my-item-sheets',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  removeItemFromSheet = (sheetId: string, itemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'DELETE',
      url: '/api/app/item-sheet/item-from-sheet',
      params: { sheetId, itemId },
    },
    { apiName: this.apiName,...config });
  

  unassignFromRental = (sheetId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ItemSheetDto>({
      method: 'POST',
      url: `/api/app/item-sheet/unassign-from-rental/${sheetId}`,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
