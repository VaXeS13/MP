import type { ApplyPromotionToCartInput, CalculateDiscountInput, CalculateDiscountOutput, CreatePromotionDto, GetPromotionsInput, PromotionDto, UpdatePromotionDto, ValidatePromoCodeInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PromotionService {
  apiName = 'Default';
  

  activate = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PromotionDto>({
      method: 'POST',
      url: `/api/app/promotion/${id}/activate`,
    },
    { apiName: this.apiName,...config });
  

  applyPromotionToCart = (input: ApplyPromotionToCartInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/promotion/apply-promotion-to-cart',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  calculateDiscount = (input: CalculateDiscountInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CalculateDiscountOutput>({
      method: 'POST',
      url: '/api/app/promotion/calculate-discount',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreatePromotionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PromotionDto>({
      method: 'POST',
      url: '/api/app/promotion',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  deactivate = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PromotionDto>({
      method: 'POST',
      url: `/api/app/promotion/${id}/deactivate`,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/promotion/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PromotionDto>({
      method: 'GET',
      url: `/api/app/promotion/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActivePromotions = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, PromotionDto[]>({
      method: 'GET',
      url: '/api/app/promotion/active-promotions',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetPromotionsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PromotionDto>>({
      method: 'GET',
      url: '/api/app/promotion',
      params: { filterText: input.filterText, isActive: input.isActive, type: input.type, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  removePromotionFromCart = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: '/api/app/promotion/promotion-from-cart',
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdatePromotionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PromotionDto>({
      method: 'PUT',
      url: `/api/app/promotion/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  validatePromoCode = (input: ValidatePromoCodeInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PromotionDto>({
      method: 'POST',
      url: '/api/app/promotion/validate-promo-code',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
