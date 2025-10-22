import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { CustomerDashboardDto, CustomerNotificationDto, CustomerSalesStatisticsDto, CustomerStatisticsFilterDto, MyActiveRentalDto, QRCodeDto, RequestSettlementDto, SettlementItemDto, SettlementSummaryDto } from '../contracts/customer-dashboard/models';

@Injectable({
  providedIn: 'root',
})
export class CustomerDashboardService {
  apiName = 'Default';
  

  getBoothQRCode = (rentalId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, QRCodeDto>({
      method: 'GET',
      url: `/api/app/customer-dashboard/booth-qRCode/${rentalId}`,
    },
    { apiName: this.apiName,...config });
  

  getDashboard = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, CustomerDashboardDto>({
      method: 'GET',
      url: '/api/app/customer-dashboard/dashboard',
    },
    { apiName: this.apiName,...config });
  

  getMyActiveRentals = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, MyActiveRentalDto[]>({
      method: 'GET',
      url: '/api/app/customer-dashboard/my-active-rentals',
    },
    { apiName: this.apiName,...config });


  getMyActiveRentalsPaged = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<MyActiveRentalDto>>({
      method: 'GET',
      url: '/api/app/customer-dashboard/my-active-rentals-paged',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });


  getMyNotifications = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CustomerNotificationDto>>({
      method: 'GET',
      url: '/api/app/customer-dashboard/my-notifications',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getMySettlements = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<SettlementItemDto>>({
      method: 'GET',
      url: '/api/app/customer-dashboard/my-settlements',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getSalesStatistics = (filter: CustomerStatisticsFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CustomerSalesStatisticsDto>({
      method: 'GET',
      url: '/api/app/customer-dashboard/sales-statistics',
      params: { startDate: filter.startDate, endDate: filter.endDate, rentalId: filter.rentalId },
    },
    { apiName: this.apiName,...config });
  

  getSettlementSummary = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, SettlementSummaryDto>({
      method: 'GET',
      url: '/api/app/customer-dashboard/settlement-summary',
    },
    { apiName: this.apiName,...config });
  

  markAllNotificationsAsRead = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/customer-dashboard/mark-all-notifications-as-read',
    },
    { apiName: this.apiName,...config });
  

  markNotificationAsRead = (notificationId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/customer-dashboard/mark-notification-as-read/${notificationId}`,
    },
    { apiName: this.apiName,...config });
  

  requestSettlement = (input: RequestSettlementDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SettlementItemDto>({
      method: 'POST',
      url: '/api/app/customer-dashboard/request-settlement',
      body: input,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
