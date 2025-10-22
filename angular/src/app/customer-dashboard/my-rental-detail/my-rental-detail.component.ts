import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MyRentalService, MyItemService } from '@proxy/application/customer-dashboard';
import type {
  MyRentalDetailDto,
  MyItemDto,
} from '@proxy/application/contracts/customer-dashboard';
import type { PriceBreakdownDto } from '@proxy/application/contracts/rentals/models';

@Component({
  selector: 'app-my-rental-detail',
  templateUrl: './my-rental-detail.component.html',
  styleUrls: ['./my-rental-detail.component.scss'],
  standalone: false,
})
export class MyRentalDetailComponent implements OnInit {
  rental: MyRentalDetailDto | null = null;
  items: MyItemDto[] = [];
  loading = false;
  itemsLoading = false;
  activeTab = 0;

  private statusMap: { [key: string]: string } = {
    Draft: 'Projekt',
    Active: 'Aktywny',
    Expired: 'Wygasły',
    Cancelled: 'Anulowany',
    Extended: 'Przedłużony',
  };

  constructor(
    private myRentalService: MyRentalService,
    private myItemService: MyItemService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const rentalId = this.route.snapshot.paramMap.get('id');
    if (rentalId) {
      this.loadRentalDetail(rentalId);
      this.loadItems(rentalId);
    } else {
      this.router.navigate(['/rentals/my-rentals']);
    }
  }

  loadRentalDetail(rentalId: string): void {
    this.loading = true;
    this.myRentalService.getMyRentalDetail(rentalId).subscribe({
      next: (result) => {
        this.rental = result;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading rental detail:', error);
        this.loading = false;
        this.router.navigate(['/rentals/my-rentals']);
      },
    });
  }

  loadItems(rentalId: string): void {
    this.itemsLoading = true;
    this.myItemService
      .getMyItems({
        rentalId: rentalId,
        skipCount: 0,
        maxResultCount: 100,
      })
      .subscribe({
        next: (result) => {
          this.items = result.items || [];
          this.itemsLoading = false;
        },
        error: (error) => {
          console.error('Error loading items:', error);
          this.itemsLoading = false;
        },
      });
  }

  getStatusLabel(status: string | undefined): string {
    if (!status) return 'N/A';
    return this.statusMap[status] || status;
  }

  getStatusSeverity(status: string | undefined): string {
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
      currency: 'PLN',
    }).format(amount);
  }

  hasDiscount(): boolean {
    return (this.rental?.discountAmount || 0) > 0;
  }

  getItemStatusLabel(status: string | undefined): string {
    if (!status) return 'N/A';
    const itemStatusMap: { [key: string]: string } = {
      Draft: 'Projekt',
      InSheet: 'W artykule',
      Available: 'Dostępny',
      Sold: 'Sprzedany',
      Reclaimed: 'Odebrany',
      Expired: 'Wygasły',
    };
    return itemStatusMap[status] || status;
  }

  getItemStatusSeverity(status: string | undefined): string {
    switch (status) {
      case 'Available':
        return 'success';
      case 'InSheet':
        return 'info';
      case 'Sold':
        return 'success';
      case 'Reclaimed':
        return 'warning';
      case 'Expired':
      case 'Draft':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  goBack(): void {
    this.router.navigate(['/customer-dashboard/my-rentals']);
  }

  /**
   * Check if rental has price breakdown with multiple periods
   */
  hasPriceBreakdown(): boolean {
    return !!(this.rental?.priceBreakdown && this.rental.priceBreakdown.items && this.rental.priceBreakdown.items.length > 1);
  }

  /**
   * Format price breakdown for tooltip display
   */
  formatPriceBreakdown(): string {
    if (!this.rental?.priceBreakdown) {
      return '';
    }

    const breakdown = this.rental.priceBreakdown;
    let html = '<div class="price-breakdown-tooltip">';
    html += '<div class="breakdown-title"><strong>Rozliczenie ceny:</strong></div>';

    breakdown.items.forEach(item => {
      html += '<div class="breakdown-item">';
      html += `• ${item.count} × ${item.days} ${item.days === 1 ? 'dzień' : 'dni'} = `;
      html += `${this.formatCurrency(item.subtotal)} (${this.formatCurrency(item.pricePerPeriod)} każdy)`;
      html += '</div>';
    });

    html += '<hr class="breakdown-divider">';
    html += `<div class="breakdown-total"><strong>Suma: ${this.formatCurrency(breakdown.totalPrice)}</strong></div>`;
    html += '</div>';

    return html;
  }
}
