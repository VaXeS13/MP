import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { ExtensionCostCalculationDto, GetMyRentalsDto, MyActiveRentalDto, MyRentalCalendarDto, MyRentalDetailDto, RentalActivityDto, RentalExtensionResultDto, RequestRentalExtensionDto } from '../contracts/customer-dashboard/models';

@Injectable({
  providedIn: 'root',
})
export class MyRentalService {
  apiName = 'Default';
  

  calculateExtensionCost = (rentalId: string, days: number, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ExtensionCostCalculationDto>({
      method: 'POST',
      url: `/api/app/my-rental/calculate-extension-cost/${rentalId}`,
      params: { days },
    },
    { apiName: this.apiName,...config });
  

  cancelMyRental = (id: string, reason: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/my-rental/${id}/cancel-my-rental`,
      params: { reason },
    },
    { apiName: this.apiName,...config });
  

  getMyRentalCalendar = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyRentalCalendarDto>({
      method: 'GET',
      url: '/api/app/my-rental/my-rental-calendar',
    },
    { apiName: this.apiName,...config });
  

  getMyRentalDetail = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyRentalDetailDto>({
      method: 'GET',
      url: `/api/app/my-rental/${id}/my-rental-detail`,
    },
    { apiName: this.apiName,...config });
  

  getMyRentals = (input: GetMyRentalsDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<MyActiveRentalDto>>({
      method: 'GET',
      url: '/api/app/my-rental/my-rentals',
      params: { status: input.status, startDateFrom: input.startDateFrom, startDateTo: input.startDateTo, includeCompleted: input.includeCompleted, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getRentalActivity = (rentalId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalActivityDto[]>({
      method: 'GET',
      url: `/api/app/my-rental/rental-activity/${rentalId}`,
    },
    { apiName: this.apiName,...config });
  

  requestExtension = (input: RequestRentalExtensionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, RentalExtensionResultDto>({
      method: 'POST',
      url: '/api/app/my-rental/request-extension',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
