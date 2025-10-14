import type { AdminManageBoothRentalDto, BoothCalendarRequestDto, BoothCalendarResponseDto, CreateMyRentalDto, CreateRentalDto, CreateRentalWithPaymentDto, CreateRentalWithPaymentResultDto, ExtendRentalDto, GetRentalListDto, MaxExtensionDateResponseDto, PaymentDto, RentalDto, RentalListDto, UpdateRentalDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class RentalService {
  apiName = 'Default';
  

  adminManageBoothRental = (input: AdminManageBoothRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: '/api/app/rental/admin-manage-booth-rental',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  calculateCost = (boothId: string, boothTypeId: string, startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, number>({
      method: 'POST',
      url: '/api/app/rental/calculate-cost',
      params: { boothId, boothTypeId, startDate, endDate },
    },
    { apiName: this.apiName,...config });
  

  cancelRental = (id: string, reason: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rental/${id}/cancel-rental`,
      params: { reason },
    },
    { apiName: this.apiName,...config });
  

  checkAvailability = (boothId: string, startDate: string, endDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, boolean>({
      method: 'POST',
      url: `/api/app/rental/check-availability/${boothId}`,
      params: { startDate, endDate },
    },
    { apiName: this.apiName,...config });
  

  completeRental = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rental/${id}/complete-rental`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: '/api/app/rental',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createMyRental = (input: CreateMyRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: '/api/app/rental/my-rental',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  createMyRentalWithPayment = (input: CreateRentalWithPaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CreateRentalWithPaymentResultDto>({
      method: 'POST',
      url: '/api/app/rental/my-rental-with-payment',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/rental/${id}`,
    },
    { apiName: this.apiName,...config });
  

  extendRental = (id: string, input: ExtendRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rental/${id}/extend-rental`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'GET',
      url: `/api/app/rental/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActiveRentalForBooth = (boothId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'GET',
      url: `/api/app/rental/active-rental-for-booth/${boothId}`,
    },
    { apiName: this.apiName,...config });
  

  getActiveRentals = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalListDto[]>({
      method: 'GET',
      url: '/api/app/rental/active-rentals',
    },
    { apiName: this.apiName,...config });
  

  getBoothCalendar = (input: BoothCalendarRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothCalendarResponseDto>({
      method: 'GET',
      url: '/api/app/rental/booth-calendar',
      params: { boothId: input.boothId, startDate: input.startDate, endDate: input.endDate, excludeCartId: input.excludeCartId },
    },
    { apiName: this.apiName,...config });
  

  getExpiredRentals = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalListDto[]>({
      method: 'GET',
      url: '/api/app/rental/expired-rentals',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetRentalListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<RentalListDto>>({
      method: 'GET',
      url: '/api/app/rental',
      params: { filter: input.filter, status: input.status, userId: input.userId, boothId: input.boothId, fromDate: input.fromDate, toDate: input.toDate, isOverdue: input.isOverdue, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMaxExtensionDate = (boothId: string, currentRentalEndDate: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MaxExtensionDateResponseDto>({
      method: 'GET',
      url: `/api/app/rental/max-extension-date/${boothId}`,
      params: { currentRentalEndDate },
    },
    { apiName: this.apiName,...config });
  

  getMyRentals = (input: GetRentalListDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<RentalListDto>>({
      method: 'GET',
      url: '/api/app/rental/my-rentals',
      params: { filter: input.filter, status: input.status, userId: input.userId, boothId: input.boothId, fromDate: input.fromDate, toDate: input.toDate, isOverdue: input.isOverdue, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getOverdueRentals = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalListDto[]>({
      method: 'GET',
      url: '/api/app/rental/overdue-rentals',
    },
    { apiName: this.apiName,...config });
  

  pay = (id: string, input: PaymentDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rental/${id}/pay`,
      body: input,
    },
    { apiName: this.apiName,...config });
  

  startRental = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'POST',
      url: `/api/app/rental/${id}/start-rental`,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: UpdateRentalDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalDto>({
      method: 'PUT',
      url: `/api/app/rental/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
