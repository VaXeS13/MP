import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-customer-dashboard',
  templateUrl: './customer-dashboard.component.html',
  styleUrls: ['./customer-dashboard.component.scss'],
  standalone: false
})
export class CustomerDashboardComponent implements OnInit {
  loading = false;

  // Dashboard data (simplified - will be connected to API later)
  overview = {
    totalActiveRentals: 2,
    totalItemsForSale: 15,
    totalItemsSold: 8,
    totalSalesAmount: 1250.00,
    availableForWithdrawal: 950.00,
    daysUntilNextRentalExpiration: 12
  };

  activeRentals: any[] = [];
  recentSales: any[] = [];
  salesStats = {
    todaySales: 0,
    weekSales: 450.00,
    monthSales: 1250.00,
    last30DaysSales: []
  };

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  private loadDashboardData(): void {
    this.loading = true;

    // Mock data - replace with actual API call
    this.activeRentals = [
      {
        rentalId: '1',
        boothNumber: 'A-101',
        startDate: new Date(2025, 0, 1),
        endDate: new Date(2025, 2, 31),
        daysRemaining: 45,
        totalItems: 10,
        soldItems: 3,
        totalSales: 450.00,
        isExpiringSoon: false
      },
      {
        rentalId: '2',
        boothNumber: 'B-205',
        startDate: new Date(2025, 1, 1),
        endDate: new Date(2025, 1, 28),
        daysRemaining: 12,
        totalItems: 5,
        soldItems: 5,
        totalSales: 800.00,
        isExpiringSoon: true
      }
    ];

    this.recentSales = [
      {
        itemName: 'Vintage Lamp',
        salePrice: 150.00,
        soldAt: new Date(2025, 0, 15),
        boothNumber: 'A-101'
      },
      {
        itemName: 'Wooden Chair',
        salePrice: 120.00,
        soldAt: new Date(2025, 0, 14),
        boothNumber: 'A-101'
      }
    ];

    setTimeout(() => {
      this.loading = false;
    }, 500);
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

  trackBySaleIndex(index: number, sale: any): number {
    return index;
  }
}
