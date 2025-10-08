import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BoothService } from '../../services/booth.service';
import { BoothDto, GetBoothListDto, BoothStatus } from '../../shared/models/booth.model';
import { MessageService } from 'primeng/api';
import { PagedResultDto } from '@abp/ng.core';

@Component({
  selector: 'app-rental-booth-selection',
  standalone: false,
  templateUrl: './rental-booth-selection.component.html',
  styleUrl: './rental-booth-selection.component.scss'
})
export class RentalBoothSelectionComponent implements OnInit {
  booths: BoothDto[] = [];
  loading = false;
  pageIndex = 0;
  pageSize = 12;
  totalCount = 0;
  searchFilter = '';

  constructor(
    private boothService: BoothService,
    public router: Router,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadBooths();
  }

  loadBooths(): void {
    this.loading = true;

    const input: GetBoothListDto = {
      skipCount: this.pageIndex * this.pageSize,
      maxResultCount: this.pageSize,
      filter: this.searchFilter
      // Show all booth statuses by default
    };

    this.boothService.getList(input).subscribe({
      next: (result: PagedResultDto<BoothDto>) => {
        this.booths = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading booths:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load available booths'
        });
        this.loading = false;
      }
    });
  }

  onPageChange(event: any): void {
    this.pageIndex = event.first / event.rows;
    this.pageSize = event.rows;
    this.loadBooths();
  }

  onSearchChange(): void {
    this.pageIndex = 0;
    this.loadBooths();
  }

  selectBooth(booth: BoothDto): void {
    this.router.navigate(['/rentals/book', booth.id]);
  }

  getStatusClass(status: BoothStatus): string {
    switch (status) {
      case BoothStatus.Available:
        return 'badge bg-success';
      case BoothStatus.Reserved:
        return 'badge bg-info';
      case BoothStatus.Rented:
        return 'badge bg-warning';
      case BoothStatus.Maintenance:
        return 'badge bg-danger';
      default:
        return 'badge bg-secondary';
    }
  }

  getStatusText(status: BoothStatus): string {
    switch (status) {
      case BoothStatus.Available:
        return 'Available';
      case BoothStatus.Reserved:
        return 'Reserved';
      case BoothStatus.Rented:
        return 'Rented';
      case BoothStatus.Maintenance:
        return 'Maintenance';
      default:
        return 'Unknown';
    }
  }

  isBoothSelectable(booth: BoothDto): boolean {
    return booth.status === BoothStatus.Available;
  }

  getBoothUnavailabilityReason(booth: BoothDto): string {
    switch (booth.status) {
      case BoothStatus.Reserved:
        return 'Reserved';
      case BoothStatus.Rented:
        return 'Rented';
      case BoothStatus.Maintenance:
        return 'Under Maintenance';
      default:
        return 'Unavailable';
    }
  }

  trackByBoothId(index: number, booth: BoothDto): string {
    return booth.id;
  }
}
