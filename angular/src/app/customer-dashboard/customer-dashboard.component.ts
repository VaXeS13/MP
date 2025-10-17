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

@Component({
  selector: 'app-customer-dashboard',
  templateUrl: './customer-dashboard.component.html',
  styleUrls: ['./customer-dashboard.component.scss'],
  standalone: false
})
export class CustomerDashboardComponent implements OnInit {
  loading = false;
  errorMessage: string | null = null;

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
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading dashboard data:', error);
        this.errorMessage = 'Nie udało się załadować danych dashboardu. Spróbuj ponownie później.';
        this.loading = false;
      }
    });
  }

  navigateToItems(): void {
    this.router.navigate(['/customer-dashboard/my-items']);
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
