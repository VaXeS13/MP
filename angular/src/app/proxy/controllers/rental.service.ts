import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { P24NotificationDto } from '../application/contracts/payments/models';
import type { PaymentStatus } from '../domain/rentals/payment-status.enum';
import type { IActionResult } from '../microsoft/asp-net-core/mvc/models';
import type { AdminManageBoothRentalDto, BoothCalendarRequestDto, BoothCalendarResponseDto, CreateMyRentalDto, CreateRentalDto, CreateRentalWithPaymentDto, CreateRentalWithPaymentResultDto, ExtendRentalDto, GetRentalListDto, MaxExtensionDateResponseDto, PaymentDto, RentalDto, RentalListDto, UpdateRentalDto } from '../rentals/models';

@Injectable({
  providedIn: 'root',
})
export class RentalService {
  apiName = 'Default';
  

  adminManageBoothRental = (input: AdminManageBoothRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: '/api/app/rentals/admin-manage',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  calculateCost = (boothId: string, boothTypeId: string, startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, number>({
      method: 'GET',
      url: '/api/app/rentals/calculate-cost',
      params: { boothId, boothTypeId, startDate, endDate },
    },
    { apiName: this.apiName,...config });
  

  cancelRental = (id: string, reason: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rentals/${id}/cancel`,
      body: reason,
    },
    { apiName: this.apiName,...config });
  

  checkAvailability = (boothId: string, startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'GET',
      url: '/api/app/rentals/check-availability',
      params: { boothId, startDate, endDate },
    },
    { apiName: this.apiName,...config });
  

  completeRental = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rentals/${id}/complete`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: '/api/app/rentals',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createMyRental = (input: CreateMyRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: '/api/app/rentals/my-rental',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createMyRentalWithPayment = (input: CreateRentalWithPaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CreateRentalWithPaymentResultDto>({
      method: 'POST',
      url: '/api/app/rentals/create-with-payment',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/rentals/${id}`,
    },
    { apiName: this.apiName,...config });
  

  extendRental = (id: string, input: ExtendRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rentals/${id}/extend`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'GET',
      url: `/api/app/rentals/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActiveRentalForBooth = (boothId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'GET',
      url: `/api/app/rentals/active-for-booth/${boothId}`,
    },
    { apiName: this.apiName,...config });
  

  getActiveRentals = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalListDto[]>({
      method: 'GET',
      url: '/api/app/rentals/active',
    },
    { apiName: this.apiName,...config });
  

  getBoothCalendar = (input: BoothCalendarRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothCalendarResponseDto>({
      method: 'POST',
      url: '/api/app/rentals/booth-calendar',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  getExpiredRentals = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalListDto[]>({
      method: 'GET',
      url: '/api/app/rentals/expired',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetRentalListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<RentalListDto>>({
      method: 'GET',
      url: '/api/app/rentals',
      params: { filter: input.filter, status: input.status, userId: input.userId, boothId: input.boothId, fromDate: input.fromDate, toDate: input.toDate, isOverdue: input.isOverdue, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMaxExtensionDate = (boothId: string, currentRentalEndDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MaxExtensionDateResponseDto>({
      method: 'GET',
      url: '/api/app/rentals/max-extension-date',
      params: { boothId, currentRentalEndDate },
    },
    { apiName: this.apiName,...config });
  

  getMyRentals = (input: GetRentalListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<RentalListDto>>({
      method: 'GET',
      url: '/api/app/rentals/my-rentals',
      params: { filter: input.filter, status: input.status, userId: input.userId, boothId: input.boothId, fromDate: input.fromDate, toDate: input.toDate, isOverdue: input.isOverdue, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getOverdueRentals = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalListDto[]>({
      method: 'GET',
      url: '/api/app/rentals/overdue',
    },
    { apiName: this.apiName,...config });
  

  getPaymentStatus = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentStatus>({
      method: 'GET',
      url: `/api/app/rentals/${id}/payment-status`,
    },
    { apiName: this.apiName,...config });
  

  initiatePayment = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, string>({
      method: 'POST',
      responseType: 'text',
      url: `/api/app/rentals/${id}/initiate-payment`,
    },
    { apiName: this.apiName,...config });
  

  pay = (id: string, input: PaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rentals/${id}/pay`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  paymentNotification = (notification: P24NotificationDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, IActionResult>({
      method: 'POST',
      url: '/api/app/rentals/payment/notification',
    },
    { apiName: this.apiName,...config });
  

  startRental = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rentals/${id}/start`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'PUT',
      url: `/api/app/rentals/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
