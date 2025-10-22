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
  pageSize = 10;
  totalRentals = 0;
  currentPage = 0;
  filterStatus: string | null = null;

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
      skipCount: this.currentPage * this.pageSize,
      maxResultCount: this.pageSize,
      includeCompleted: true
    }).subscribe({
      next: (result) => {
        this.rentals = result.items || [];
        this.totalRentals = result.totalCount || this.rentals.length;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading rentals:', error);
        this.loading = false;
      }
    });
  }

  onPageChange(event: any): void {
    this.currentPage = event.first / event.rows;
    this.pageSize = event.rows;
    this.loadRentals();
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
