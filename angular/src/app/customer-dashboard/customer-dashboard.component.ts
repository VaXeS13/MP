import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CustomerDashboardService } from '@proxy/application/customer-dashboard';
import {
  CustomerOverviewDto,
  MyActiveRentalDto,
  RecentItemSaleDto,
  CustomerDashboardDto,
  CustomerSalesStatisticsDto
} from '@proxy/application/contracts/customer-dashboard';
import { PagedAndSortedResultRequestDto } from '@abp/ng.core';

@Component({
  selector: 'app-customer-dashboard',
  templateUrl: './customer-dashboard.component.html',
  styleUrls: ['./customer-dashboard.component.scss'],
  standalone: false
})
export class CustomerDashboardComponent implements OnInit {
  loading = false;
  errorMessage: string | null = null;
  pageSize = 10;
  totalRentals = 0;
  currentPage = 0;

  overview: CustomerOverviewDto = {
    totalActiveRentals: 0,
    totalItemsForSale: 0,
    totalItemsSold: 0,
    totalSalesAmount: 0,
    totalCommissionPaid: 0,
    availableForWithdrawal: 0,
    daysUntilNextRentalExpiration: 0,
    hasExpiringRentals: false,
    hasPendingSettlements: false
  };

  activeRentals: MyActiveRentalDto[] = [];
  recentSales: RecentItemSaleDto[] = [];
  salesStats: CustomerSalesStatisticsDto = {
    todaySales: 0,
    weekSales: 0,
    monthSales: 0,
    allTimeSales: 0,
    todayItemsSold: 0,
    weekItemsSold: 0,
    monthItemsSold: 0,
    allTimeItemsSold: 0,
    averageSalePrice: 0,
    highestSalePrice: 0,
    lowestSalePrice: 0,
    last30DaysSales: [],
    salesByCategory: [],
    monthlyGrowthPercentage: 0
  };

  constructor(
    private router: Router,
    private customerDashboardService: CustomerDashboardService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.loading = true;
    this.errorMessage = null;

    this.customerDashboardService.getDashboard().subscribe({
      next: (data: CustomerDashboardDto) => {
        this.overview = data.overview;
        this.activeRentals = data.activeRentals;
        this.recentSales = data.recentSales;
        this.salesStats = data.salesStatistics;
        this.totalRentals = data.activeRentals.length;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        this.errorMessage = 'Nie udało się załadować danych dashboardu. Spróbuj ponownie później.';
        this.loading = false;
      }
    });
  }

  onRentalsPageChange(event: any): void {
    this.currentPage = event.first / event.rows;
    this.pageSize = event.rows;
    this.loadDashboardData();
  }

  formatTimeRemaining(rental: MyActiveRentalDto): string {
    const now = new Date();
    const endDate = new Date(rental.endDate);
    const diff = endDate.getTime() - now.getTime();

    if (diff < 0) return 'Wygasło';

    const hours = Math.floor(diff / (1000 * 60 * 60));
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const seconds = Math.floor((diff % (1000 * 60)) / 1000);

    if (hours < 24) {
      return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }
    return `${rental.daysRemaining} dni`;
  }

  navigateToAddItem(): void {
    this.router.navigate(['/items/list']);
  }

  navigateToItems(): void {
    this.router.navigate(['/customer-dashboard/my-items']);
  }

  navigateToSoldItems(): void {
    this.router.navigate(['/customer-dashboard/my-items'], { queryParams: { filterStatus: 'Sold' } });
  }

  navigateToRentals(): void {
    this.router.navigate(['/customer-dashboard/my-rentals']);
  }

  navigateToSettlements(): void {
    this.router.navigate(['/customer-dashboard/settlements']);
  }

  getStatusClass(rental: any): string {
    if (rental.isExpiringSoon) {
      return 'warning';
    }
    return 'success';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount);
  }

  trackBySaleIndex(index: number, sale: RecentItemSaleDto): string {
    return sale.itemId ? sale.itemId.toString() : index.toString();
  }
}
