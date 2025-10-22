import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';
import { BoothService } from '../../proxy/booths/booth.service';
import { BoothSignalRService } from '../../services/booth-signalr.service';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { BoothListDto, GetBoothListDto } from '../../proxy/booths/models';
import { BoothStatus } from '../../proxy/domain/booths/booth-status.enum';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { LocalizationService } from '@abp/ng.core';
import { Subscription } from 'rxjs';
import { IdentityUserService } from '@abp/ng.identity/proxy';

@Component({
  standalone: false,
  selector: 'app-booth-list',
  templateUrl: './booth-list.component.html',
  styleUrls: ['./booth-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [ // Dodaj blok animacji
    trigger('slideInOut', [
      state('in', style({
        transform: 'translateY(0)',
        opacity: 1,
        'max-height': '200px', // Ustaw większą wartość, jeśli potrzeba
        visibility: 'visible'
      })),
      state('out', style({
        transform: 'translateY(-10px)',
        opacity: 0,
        'max-height': '0',
        visibility: 'hidden'
      })),
      transition('in => out', [animate('300ms ease-in')]),
      transition('out => in', [animate('300ms ease-out')])
    ])
  ]
})
export class BoothListComponent implements OnInit, OnDestroy {
  booths: BoothListDto[] = [];
  totalCount = 0;
  loading = false;

  currentLocale: string = 'en-US';
  tenantCurrencyCode: string = 'PLN';
  showAdvancedFilters = false;
  private boothUpdatesSubscription?: Subscription;
  // Filters
  filterText = '';
  selectedStatus: BoothStatus | null = null;
  
  // Pagination
  first = 0;
  rows = 10;
  
  // Dialog
  displayCreateDialog = false;
  displayEditDialog = false;
  displayManualReservationDialog = false;
  displayExtendDialog = false;
  displayAdminRentalDialog = false; // New unified dialog

  // Manual Reservation (keep for backward compatibility)
  availableUsers: any[] = [];
  selectedUserId: string = '';
  reservationStartDate: Date | null = null;
  reservationEndDate: Date | null = null;
  reservationStatus: BoothStatus = BoothStatus.Reserved;
  statusOptionsForReservation = [
    { label: 'Reserved', value: BoothStatus.Reserved },
    { label: 'Rented', value: BoothStatus.Rented }
  ];

  // View mode
  viewMode: 'grid' | 'list' = 'list';
  selectedBooth: BoothListDto | null = null;
  selectedBoothIdForExtension: string | null = null;
  rentalManagementMode: 'new' | 'extend' = 'new'; // Mode for unified dialog
  
  // Dropdowns
  statusOptions = [
    { label: this.localizationService.instant('::Status:AllStatuses'), value: null },
    { label: this.localizationService.instant('::Status:Available'), value: BoothStatus.Available },
    { label: this.localizationService.instant('::Status:Reserved'), value: BoothStatus.Reserved },
    { label: this.localizationService.instant('::Status:Rented'), value: BoothStatus.Rented },
    { label: this.localizationService.instant('::Status:Maintenance'), value: BoothStatus.Maintenance }
  ];


  constructor(
    private boothService: BoothService,
    private boothSignalRService: BoothSignalRService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private localizationService: LocalizationService,
    private identityUserService: IdentityUserService,
    private tenantCurrencyService: TenantCurrencyService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.currentLocale = this.localizationService.currentLang;

    // Load tenant currency
    this.tenantCurrencyService.getCurrency().subscribe(result => {
      this.tenantCurrencyCode = this.tenantCurrencyService.getCurrencyName(result.currency);
      this.cdr.markForCheck();
    });

    this.loadBooths();

    // Subscribe to booth status updates via SignalR
    console.log('BoothList: Setting up booth updates subscription...');
    this.boothUpdatesSubscription = this.boothSignalRService.boothUpdates.subscribe(update => {
      console.log('BoothList: ✅ Received booth status update via SignalR:', update);
      // Refresh booth list to reflect changes
      console.log('BoothList: Reloading booth list due to status update...');
      this.loadBooths();
      this.cdr.markForCheck();
    });
    console.log('BoothList: ✅ Booth updates subscription active');
  }

  ngOnDestroy(): void {
    if (this.boothUpdatesSubscription) {
      this.boothUpdatesSubscription.unsubscribe();
    }
  }
 toggleAdvancedFilters(): void {
    this.showAdvancedFilters = !this.showAdvancedFilters;
  }
  loadBooths(): void {
    this.loading = true;
    
    const input: GetBoothListDto = {
      filter: this.filterText || undefined,
      status: this.selectedStatus || undefined,
      skipCount: this.first,
      maxResultCount: this.rows
    };

this.boothService.getList(input).subscribe({
  next: (result) => {
    this.booths = result.items;
    this.totalCount = result.totalCount;
    this.loading = false;
    this.cdr.markForCheck();
  },
  error: (error) => {
    this.messageService.add({
      severity: 'error',
      summary: this.localizationService.instant('::Messages:Error'),
      detail: this.localizationService.instant('::Booth:LoadError')
    });
    this.loading = false;
    this.cdr.markForCheck();
  }
});

  }

  onFilter(): void {
    this.first = 0;
    this.loadBooths();
  }

  onPageChange(event: any): void {
    this.first = event.first;
    this.rows = event.rows;
    this.loadBooths();
  }

  showCreateDialog(): void {
    this.displayCreateDialog = true;
  }

  showEditDialog(booth: BoothListDto): void {
    this.selectedBooth = booth;
    this.displayEditDialog = true;
  }

  onBoothCreated(): void {
    this.displayCreateDialog = false;
    this.loadBooths();
    this.messageService.add({
      severity: 'success',
      summary: this.localizationService.instant('::Messages:Success'),
      detail: this.localizationService.instant('::Booth:CreatedSuccessfully')
    });
  }

  onBoothUpdated(): void {
    this.displayEditDialog = false;
    this.selectedBooth = null;
    this.loadBooths();
    this.messageService.add({
      severity: 'success',
      summary: this.localizationService.instant('::Messages:Success'),
      detail: this.localizationService.instant('::Booth:UpdatedSuccessfully')
    });
  }

  setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode = mode;
  }

  deleteBooth(booth: BoothListDto): void {
    this.confirmationService.confirm({
      message: this.localizationService.instant('::Booth:DeleteConfirmation', booth.number),
      header: this.localizationService.instant('::Messages:Confirmation'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.boothService.delete(booth.id).subscribe({
          next: () => {
            this.loadBooths();
            this.messageService.add({
              severity: 'success',
              summary: this.localizationService.instant('::Messages:Success'),
              detail: this.localizationService.instant('::Booth:DeletedSuccessfully')
            });
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.localizationService.instant('::Messages:Error'),
              detail: this.localizationService.instant('::Booth:DeleteError')
            });
          }
        });
      }
    });
  }

  changeBoothStatus(booth: BoothListDto, newStatus: BoothStatus): void {
    this.boothService.changeStatus(booth.id, newStatus).subscribe({
      next: () => {
        this.loadBooths();
        this.messageService.add({
          severity: 'success',
          summary: this.localizationService.instant('::Messages:Success'),
          detail: this.localizationService.instant('::Booth:StatusChangedSuccessfully')
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.localizationService.instant('::Messages:Error'),
          detail: this.localizationService.instant('::Booth:StatusChangeError')
        });
      }
    });
  }

  getStatusSeverity(status: BoothStatus): string {
    switch (status) {
      case BoothStatus.Available: return 'success';
      case BoothStatus.Reserved: return 'info';
      case BoothStatus.Rented: return 'warning';
      case BoothStatus.Maintenance: return 'danger';
      default: return 'secondary';
    }
  }

  openManualReservationDialog(booth: BoothListDto): void {
    this.selectedBooth = booth;
    this.displayManualReservationDialog = true;
    this.loadAvailableUsers();

    // Reset form
    this.selectedUserId = '';
    this.reservationStartDate = null;
    this.reservationEndDate = null;
    this.reservationStatus = BoothStatus.Reserved;
  }

  loadAvailableUsers(): void {
    this.identityUserService.getList({
      maxResultCount: 100,
      skipCount: 0
    }).subscribe({
      next: (result) => {
        this.availableUsers = result.items.map(user => ({
          id: user.id,
          userName: user.userName,
          name: user.name,
          surname: user.surname,
          email: user.email,
          displayName: `${user.name} ${user.surname} (${user.email})`
        }));
        this.cdr.markForCheck();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.localizationService.instant('::Messages:Error'),
          detail: 'Failed to load users'
        });
        this.cdr.markForCheck();
      }
    });
  }

  createManualReservation(): void {
    if (!this.selectedBooth || !this.selectedUserId || !this.reservationStartDate || !this.reservationEndDate) {
      this.messageService.add({
        severity: 'warn',
        summary: this.localizationService.instant('::Messages:Warning'),
        detail: 'Please fill all required fields'
      });
      return;
    }

    const input = {
      boothId: this.selectedBooth.id,
      userId: this.selectedUserId,
      startDate: this.reservationStartDate.toISOString(),
      endDate: this.reservationEndDate.toISOString(),
      targetStatus: this.reservationStatus
    };

    this.boothService.createManualReservation(input).subscribe({
      next: () => {
        this.displayManualReservationDialog = false;
        this.selectedBooth = null;
        this.loadBooths();
        this.messageService.add({
          severity: 'success',
          summary: this.localizationService.instant('::Messages:Success'),
          detail: this.localizationService.instant('::Booth:ManualReservationSuccess')
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.localizationService.instant('::Messages:Error'),
          detail: this.localizationService.instant('::Booth:ManualReservationError')
        });
      }
    });
  }

  openExtendDialog(booth: BoothListDto): void {
    this.selectedBoothIdForExtension = booth.id;
    this.displayExtendDialog = true;
  }

  onRentalExtended(): void {
    this.displayExtendDialog = false;
    this.selectedBoothIdForExtension = null;
    this.loadBooths();
    this.messageService.add({
      severity: 'success',
      summary: this.localizationService.instant('::Messages:Success'),
      detail: 'Rental extended successfully'
    });
  }

  // New unified rental management methods
  openAdminRentalDialog(booth: BoothListDto, mode: 'new' | 'extend'): void {
    this.selectedBooth = booth;
    this.rentalManagementMode = mode;
    this.displayAdminRentalDialog = true;
  }

  onAdminRentalCreatedOrExtended(): void {
    this.displayAdminRentalDialog = false;
    this.selectedBooth = null;
    this.loadBooths();
    this.messageService.add({
      severity: 'success',
      summary: this.localizationService.instant('::Messages:Success'),
      detail: this.rentalManagementMode === 'extend'
        ? this.localizationService.instant('::Rental:ExtendedSuccessfully')
        : this.localizationService.instant('::Rental:CreatedSuccessfully')
    });
  }

  hasActiveRental(booth: BoothListDto): boolean {
    // Check if booth has current rental information
    const rentalEndDate = booth.currentRentalEndDate || booth.rentalEndDate;

    if (!rentalEndDate) {
      return false;
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const rentalEnd = new Date(rentalEndDate);
    rentalEnd.setHours(0, 0, 0, 0);

    return rentalEnd >= today;
  }

  getPricingTooltip(periods: any[]): string {
    if (!periods || periods.length === 0) {
      return '';
    }

    const lines = periods.map(period => {
      const daysLabel = period.days === 1 ? 'dzień' : 'dni';
      const price = (period.pricePerPeriod as number).toFixed(2);
      const perDay = (period.effectivePricePerDay as number).toFixed(2);
      return `${period.days} ${daysLabel}: ${price} PLN (${perDay} zł/dzień)`;
    });

    return lines.join('\n');
  }

  trackByBoothId(index: number, booth: BoothListDto): string {
    return booth.id;
  }
}