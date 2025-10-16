import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { ExportFormat } from '../contracts/dashboard/export-format.enum';
import type { BoothOccupancyOverviewDto, CustomerAnalyticsDto, DashboardOverviewDto, FinancialOverviewDto, PaymentAnalyticsDto, PeriodFilterDto, SalesOverviewDto } from '../contracts/dashboard/models';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  apiName = 'Default';
  

  exportFinancialReport = (filter: PeriodFilterDto, format: ExportFormat, config?: Partial<Rest.Config>) =>
    this.restService.request<any, number[]>({
      method: 'POST',
      url: '/api/app/dashboard/export-financial-report',
      params: { format },
      body: filter,
    },
    { apiName: this.apiName,...config });
  

  exportSalesReport = (filter: PeriodFilterDto, format: ExportFormat, config?: Partial<Rest.Config>) =>
    this.restService.request<any, number[]>({
      method: 'POST',
      url: '/api/app/dashboard/export-sales-report',
      params: { format },
      body: filter,
    },
    { apiName: this.apiName,...config });
  

  getBoothOccupancy = (filter: PeriodFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, BoothOccupancyOverviewDto>({
      method: 'GET',
      url: '/api/app/dashboard/booth-occupancy',
      params: { startDate: filter.startDate, endDate: filter.endDate, period: filter.period },
    },
    { apiName: this.apiName,...config });
  

  getCustomerAnalytics = (filter: PeriodFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CustomerAnalyticsDto>({
      method: 'GET',
      url: '/api/app/dashboard/customer-analytics',
      params: { startDate: filter.startDate, endDate: filter.endDate, period: filter.period },
    },
    { apiName: this.apiName,...config });
  

  getFinancialReports = (filter: PeriodFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, FinancialOverviewDto>({
      method: 'GET',
      url: '/api/app/dashboard/financial-reports',
      params: { startDate: filter.startDate, endDate: filter.endDate, period: filter.period },
    },
    { apiName: this.apiName,...config });
  

  getOverview = (filter: PeriodFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DashboardOverviewDto>({
      method: 'GET',
      url: '/api/app/dashboard/overview',
      params: { startDate: filter.startDate, endDate: filter.endDate, period: filter.period },
    },
    { apiName: this.apiName,...config });
  

  getPaymentAnalytics = (filter: PeriodFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PaymentAnalyticsDto>({
      method: 'GET',
      url: '/api/app/dashboard/payment-analytics',
      params: { startDate: filter.startDate, endDate: filter.endDate, period: filter.period },
    },
    { apiName: this.apiName,...config });
  

  getSalesAnalytics = (filter: PeriodFilterDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SalesOverviewDto>({
      method: 'GET',
      url: '/api/app/dashboard/sales-analytics',
      params: { startDate: filter.startDate, endDate: filter.endDate, period: filter.period },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
