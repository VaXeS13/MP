import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MyRentalService } from '@proxy/application/customer-dashboard';
import type { MyActiveRentalDto } from '@proxy/application/contracts/customer-dashboard';

@Component({
  selector: 'app-my-rentals',
  templateUrl: './my-rentals.component.html',
  styleUrls: ['./my-rentals.component.scss'],
  standalone: false
})
export class MyRentalsComponent implements OnInit {
  rentals: MyActiveRentalDto[] = [];
  loading = false;

  private statusMap: { [key: string]: string } = {
    'Draft': 'Projekt',
    'Active': 'Aktywny',
    'Expired': 'Wygasły',
    'Cancelled': 'Anulowany',
    'Extended': 'Przedłużony'
  };

  constructor(
    private myRentalService: MyRentalService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadRentals();
  }

  loadRentals(): void {
    this.loading = true;
    this.myRentalService.getMyRentals({
      skipCount: 0,
      maxResultCount: 50,
      includeCompleted: true
    }).subscribe({
      next: (result) => {
        this.rentals = result.items || [];
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading rentals:', error);
        this.loading = false;
      }
    });
  }

  getStatusLabel(status: string): string {
    return this.statusMap[status] || status;
  }

  getStatusSeverity(status: string): string {
    switch (status) {
      case 'Active':
      case 'Extended':
        return 'success';
      case 'Draft':
        return 'warning';
      case 'Expired':
      case 'Cancelled':
        return 'danger';
      default:
        return 'info';
    }
  }

  formatCurrency(amount: number | undefined): string {
    if (amount === undefined || amount === null) {
      amount = 0;
    }
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount);
  }

  viewDetails(rentalId: string | undefined): void {
    if (rentalId) {
      this.router.navigate(['/customer-dashboard/my-rentals', rentalId]);
    }
  }

  hasDiscount(rental: MyActiveRentalDto): boolean {
    return (rental.discountAmount || 0) > 0;
  }
}
