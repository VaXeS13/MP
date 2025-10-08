import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { RentalService } from '../../services/rental.service';
import { RentalListDto, GetRentalListDto, RentalStatus } from '../../shared/models/rental.model';
import { MessageService } from 'primeng/api';
import { PagedResultDto, LocalizationService } from '@abp/ng.core';

@Component({
  selector: 'app-rental-list',
  templateUrl: './rental-list.component.html',
  styleUrls: ['./rental-list.component.scss'],
  standalone: false
})
export class RentalListComponent implements OnInit {
  rentals: RentalListDto[] = [];
  loading = false;
  pageIndex = 0;
  pageSize = 10;
  totalCount = 0;

  constructor(
    private rentalService: RentalService,
    private router: Router,
    private messageService: MessageService,
    private localization: LocalizationService
  ) {}

  ngOnInit(): void {
    this.loadRentals();
  }

  loadRentals(): void {
    this.loading = true;

    const input: GetRentalListDto = {
      skipCount: this.pageIndex * this.pageSize,
      maxResultCount: this.pageSize
    };

    this.rentalService.getMyRentals(input).subscribe({
      next: (result: PagedResultDto<RentalListDto>) => {
        this.rentals = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading rentals:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: this.localization.instant('::Rental:LoadError')
        });
        this.loading = false;
      }
    });
  }

  onPageChange(event: any): void {
    this.pageIndex = event.first / event.rows;
    this.pageSize = event.rows;
    this.loadRentals();
  }

  getStatusClass(status: RentalStatus): string {
    switch (status) {
      case RentalStatus.Draft: return 'badge bg-secondary';
      case RentalStatus.Active: return 'badge bg-success';
      case RentalStatus.Extended: return 'badge bg-info';
      case RentalStatus.Expired: return 'badge bg-warning';
      case RentalStatus.Cancelled: return 'badge bg-danger';
      default: return 'badge bg-light text-dark';
    }
  }

  getStatusIcon(status: RentalStatus): string {
    switch (status) {
      case RentalStatus.Draft: return 'fas fa-edit';
      case RentalStatus.Active: return 'fas fa-play-circle';
      case RentalStatus.Extended: return 'fas fa-clock';
      case RentalStatus.Expired: return 'fas fa-exclamation-triangle';
      case RentalStatus.Cancelled: return 'fas fa-times-circle';
      default: return 'fas fa-question-circle';
    }
  }

  viewRentalDetails(rental: RentalListDto): void {
    this.router.navigate(['/rentals', rental.id]);
  }

  canStartRental(rental: RentalListDto): boolean {
    // RentalListDto doesn't have startedAt/completedAt, so we can't determine this from list view
    // This functionality should be moved to detail view or we need to add these fields to RentalListDto
    return rental.status === RentalStatus.Draft;
  }

  canCompleteRental(rental: RentalListDto): boolean {
    // RentalListDto doesn't have startedAt/completedAt, so we can't determine this from list view
    // This functionality should be moved to detail view or we need to add these fields to RentalListDto
    return rental.status === RentalStatus.Active;
  }

  startRental(rental: RentalListDto): void {
    this.rentalService.startRental(rental.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.localization.instant('::Messages:Success'),
          detail: this.localization.instant('::Rental:StartSuccess')
        });
        this.loadRentals();
      },
      error: (error) => {
        console.error('Error starting rental:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: this.localization.instant('::Rental:StartError')
        });
      }
    });
  }

  completeRental(rental: RentalListDto): void {
    this.rentalService.completeRental(rental.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.localization.instant('::Messages:Success'),
          detail: this.localization.instant('::Rental:CompleteSuccess')
        });
        this.loadRentals();
      },
      error: (error) => {
        console.error('Error completing rental:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: this.localization.instant('::Rental:CompleteError')
        });
      }
    });
  }

  trackByRentalId(index: number, rental: RentalListDto): string {
    return rental.id;
  }
}