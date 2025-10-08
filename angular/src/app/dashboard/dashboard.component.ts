import { Component, OnInit, OnDestroy } from '@angular/core';
import { Observable, forkJoin, Subject } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { DashboardService } from '../services/dashboard.service';
import { SignalRService } from '../services/signalr.service';
import {
  DashboardOverviewDto,
  SalesOverviewDto,
  BoothOccupancyOverviewDto,
  FinancialOverviewDto,
  PaymentAnalyticsDto,
  PeriodFilterDto,
  PeriodType
} from '../shared/models/dashboard.model';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy {
  loading = false;
  private destroy$ = new Subject<void>();

  // Dashboard data
  dashboardData: DashboardOverviewDto | null = null;
  salesData: SalesOverviewDto | null = null;
  boothData: BoothOccupancyOverviewDto | null = null;
  financialData: FinancialOverviewDto | null = null;
  paymentData: PaymentAnalyticsDto | null = null;

  // Filter settings
  currentFilter: PeriodFilterDto = {
    period: PeriodType.Month
  };

  // Enum for template access
  PeriodType = PeriodType;

  constructor(
    private dashboardService: DashboardService,
    private signalRService: SignalRService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
    this.initializeSignalRListeners();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeSignalRListeners(): void {
    const dashboardHub = this.signalRService.dashboardHub;

    if (!dashboardHub) {
      console.warn('Dashboard: SignalR hub not available');
      return;
    }

    // Listen for dashboard refresh events
    dashboardHub.on('DashboardRefreshNeeded', () => {
      console.log('Dashboard: Refresh triggered by SignalR');
      this.loadDashboardData();
    });

    // Listen for specific dashboard updates
    dashboardHub.on('DashboardUpdated', (data: any) => {
      console.log('Dashboard: Live update received', data);
      this.updateDashboardData(data);
    });
  }

  private updateDashboardData(data: any): void {
    // Update dashboard metrics without full reload
    if (this.dashboardData) {
      this.dashboardData = {
        ...this.dashboardData,
        ...data
      };
    }
  }

  onPeriodChange(period: PeriodType): void {
    this.currentFilter.period = period;
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.loading = true;

    // Load all dashboard data in parallel
    forkJoin({
      overview: this.dashboardService.getOverview(this.currentFilter),
      sales: this.dashboardService.getSalesAnalytics(this.currentFilter),
      booth: this.dashboardService.getBoothOccupancy(this.currentFilter),
      financial: this.dashboardService.getFinancialReports(this.currentFilter),
      payment: this.dashboardService.getPaymentAnalytics(this.currentFilter)
    }).pipe(
      finalize(() => this.loading = false)
    ).subscribe({
      next: (data) => {
        this.dashboardData = data.overview;
        this.salesData = data.sales;
        this.boothData = data.booth;
        this.financialData = data.financial;
        this.paymentData = data.payment;
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        // TODO: Show user-friendly error message
      }
    });
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount);
  }

  formatPercentage(value: number): string {
    return `${value.toFixed(1)}%`;
  }

  getStatusBadgeClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'active':
      case 'completed':
        return 'bg-success';
      case 'processing':
        return 'bg-warning';
      case 'failed':
      case 'cancelled':
        return 'bg-danger';
      case 'expiring':
        return 'bg-warning';
      default:
        return 'bg-secondary';
    }
  }

}