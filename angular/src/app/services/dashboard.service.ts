import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  DashboardOverviewDto,
  SalesOverviewDto,
  BoothOccupancyOverviewDto,
  FinancialOverviewDto,
  CustomerAnalyticsDto,
  PaymentAnalyticsDto,
  PeriodFilterDto,
  ExportFormat
} from '../shared/models/dashboard.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private apiUrl = `${environment.apis.default.url}/api/app/dashboard`;

  constructor(private http: HttpClient) {}

  getOverview(filter: PeriodFilterDto): Observable<DashboardOverviewDto> {
    return this.http.get<DashboardOverviewDto>(`${this.apiUrl}/overview`, { params: filter as any });
  }

  getSalesAnalytics(filter: PeriodFilterDto): Observable<SalesOverviewDto> {
    return this.http.get<SalesOverviewDto>(`${this.apiUrl}/sales-analytics`, { params: filter as any });
  }

  getBoothOccupancy(filter: PeriodFilterDto): Observable<BoothOccupancyOverviewDto> {
    return this.http.get<BoothOccupancyOverviewDto>(`${this.apiUrl}/booth-occupancy`, { params: filter as any });
  }

  getFinancialReports(filter: PeriodFilterDto): Observable<FinancialOverviewDto> {
    return this.http.get<FinancialOverviewDto>(`${this.apiUrl}/financial-reports`, { params: filter as any });
  }

  getCustomerAnalytics(filter: PeriodFilterDto): Observable<CustomerAnalyticsDto> {
    return this.http.get<CustomerAnalyticsDto>(`${this.apiUrl}/customer-analytics`, { params: filter as any });
  }

  getPaymentAnalytics(filter: PeriodFilterDto): Observable<PaymentAnalyticsDto> {
    return this.http.get<PaymentAnalyticsDto>(`${this.apiUrl}/payment-analytics`, { params: filter as any });
  }

  exportSalesReport(filter: PeriodFilterDto, format: ExportFormat): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/export-sales-report`, { filter, format }, {
      responseType: 'blob'
    });
  }

  exportFinancialReport(filter: PeriodFilterDto, format: ExportFormat): Observable<Blob> {
    return this.http.post(`${this.apiUrl}/export-financial-report`, { filter, format }, {
      responseType: 'blob'
    });
  }
}