import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
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
  standalone: false,
  changeDetection: ChangeDetectionStrategy.OnPush
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
    private signalRService: SignalRService,
    private cdr: ChangeDetectorRef
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
      this.cdr.markForCheck();
    });

    // Listen for specific dashboard updates
    dashboardHub.on('DashboardUpdated', (data: any) => {
      console.log('Dashboard: Live update received', data);
      this.updateDashboardData(data);
      this.cdr.markForCheck();
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
    this.cdr.markForCheck();
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
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        this.cdr.markForCheck();
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

  trackByMethodName(index: number, method: any): string {
    return method.methodName || index.toString();
  }

  trackByTransactionId(index: number, transaction: any): string {
    return transaction.id || index.toString();
  }

  trackByRentalId(index: number, rental: any): string {
    return rental.id || index.toString();
  }

  trackByItemName(index: number, item: any): string {
    return item.itemId || item.itemName || index.toString();
  }

}