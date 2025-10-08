import type { AddToCartDto, CartDto, CheckoutCartDto, CheckoutResultDto, UpdateCartItemDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  apiName = 'Default';
  

  addItem = (input: AddToCartDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CartDto>({
      method: 'POST',
      url: '/api/app/cart/item',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  checkout = (input: CheckoutCartDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CheckoutResultDto>({
      method: 'POST',
      url: '/api/app/cart/checkout',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  clearCart = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, CartDto>({
      method: 'POST',
      url: '/api/app/cart/clear-cart',
    },
    { apiName: this.apiName,...config });
  

  getMyCart = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, CartDto>({
      method: 'GET',
      url: '/api/app/cart/my-cart',
    },
    { apiName: this.apiName,...config });
  

  removeItem = (itemId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CartDto>({
      method: 'DELETE',
      url: `/api/app/cart/item/${itemId}`,
    },
    { apiName: this.apiName,...config });
  

  updateItem = (itemId: string, input: UpdateCartItemDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CartDto>({
      method: 'PUT',
      url: `/api/app/cart/item/${itemId}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
