import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PaymentStatus } from '../../domain/rentals/payment-status.enum';

@Injectable({
  providedIn: 'root',
})
export class RentalPaymentService {
  apiName = 'Default';
  

  getPaymentStatus = (rentalId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentStatus>({
      method: 'GET',
      url: `/api/app/rental-payment/payment-status/${rentalId}`,
    },
    { apiName: this.apiName,...config });
  

  handlePaymentCallback = (transactionId: string, isSuccess: boolean, config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'POST',
      url: `/api/app/rental-payment/handle-payment-callback/${transactionId}`,
      params: { isSuccess },
    },
    { apiName: this.apiName,...config });
  

  initiatePayment = (rentalId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, string>({
      method: 'POST',
      responseType: 'text',
      url: `/api/app/rental-payment/initiate-payment/${rentalId}`,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
