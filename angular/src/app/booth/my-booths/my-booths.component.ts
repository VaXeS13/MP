import { Component, OnInit } from '@angular/core';
import { LocalizationService, PagedResultDto } from '@abp/ng.core';
import { BoothService } from '../../services/booth.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { BoothListDto, GetBoothListDto, BoothStatus } from '../../shared/models/booth.model';
import { BoothTypeDto } from '../../shared/models/booth-type.model';

@Component({
  selector: 'app-my-booths',
  templateUrl: './my-booths.component.html',
  styleUrls: ['./my-booths.component.scss'],
  standalone: false
})
export class MyBoothsComponent implements OnInit {
  booths: BoothListDto[] = [];
  loading = false;
  totalCount = 0;
  pageSize = 10;
  pageIndex = 0;
  filter = '';
  tenantCurrencyCode: string = 'PLN';

  // Enum references for template
  BoothStatus = BoothStatus;

  constructor(
    private boothService: BoothService,
    private tenantCurrencyService: TenantCurrencyService,
    private localization: LocalizationService
  ) {}

  ngOnInit(): void {
    // Load tenant currency
    this.tenantCurrencyService.getCurrency().subscribe(result => {
      this.tenantCurrencyCode = this.tenantCurrencyService.getCurrencyName(result.currency);
    });

    this.loadBooths();
  }

  loadBooths(): void {
    this.loading = true;

    const input: GetBoothListDto = {
      skipCount: this.pageIndex * this.pageSize,
      maxResultCount: this.pageSize,
      filter: this.filter,
      sorting: 'Number'
    };

    this.boothService.getMyBooths(input).subscribe({
      next: (result: PagedResultDto<BoothListDto>) => {
        this.booths = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
        console.log(result.items)
      },
      error: (error) => {
        console.error('Error loading my booths:', error);
        this.loading = false;
      }
    });
  }

  onSearch(): void {
    this.pageIndex = 0;
    this.loadBooths();
  }

  onPageChange(pageIndex: number): void {
    this.pageIndex = pageIndex;
    this.loadBooths();
  }

  getStatusBadgeClass(status: BoothStatus): string {
    switch (status) {
      case BoothStatus.Available:
        return 'badge bg-success';
      case BoothStatus.Reserved:
        return 'badge bg-info';
      case BoothStatus.Rented:
        return 'badge bg-primary';
      case BoothStatus.Maintenance:
        return 'badge bg-warning';
      default:
        return 'badge bg-secondary';
    }
  }

  getStatusDisplayName(status: BoothStatus): string {
    switch (status) {
      case BoothStatus.Available:
        return this.localization.instant('::Status:Available');
      case BoothStatus.Reserved:
        return this.localization.instant('::Status:Reserved');
      case BoothStatus.Rented:
        return this.localization.instant('::Status:Rented');
      case BoothStatus.Maintenance:
        return this.localization.instant('::Status:Maintenance');
      default:
        return this.localization.instant('::Common:Unknown');
    }
  }

  // Note: Booth types are now managed separately and chosen during rental
  // This method is no longer needed as booths don't have fixed types

  getMaxDisplayed(): number {
    return Math.min((this.pageIndex + 1) * this.pageSize, this.totalCount);
  }
}