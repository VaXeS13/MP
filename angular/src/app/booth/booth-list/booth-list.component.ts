import { Component, OnInit, OnDestroy } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';
import { BoothService } from '../../services/booth.service';
import { BoothSignalRService } from '../../services/booth-signalr.service';
import { BoothDto, BoothStatus, GetBoothListDto } from '../../shared/models/booth.model';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { LocalizationService } from '@abp/ng.core';
import { Subscription } from 'rxjs';
import { IdentityUserService } from '@abp/ng.identity/proxy';

@Component({
  standalone: false,
  selector: 'app-booth-list',
  templateUrl: './booth-list.component.html',
  styleUrls: ['./booth-list.component.scss'],
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
  booths: BoothDto[] = [];
  totalCount = 0;
  loading = false;

  currentLocale: string = 'en-US';
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

  // Manual Reservation
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
  selectedBooth: BoothDto | null = null;
  
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
    private identityUserService: IdentityUserService
  ) {}

  ngOnInit(): void {
    this.currentLocale = this.localizationService.currentLang;
    this.loadBooths();

    // Subscribe to booth status updates via SignalR
    console.log('BoothList: Setting up booth updates subscription...');
    this.boothUpdatesSubscription = this.boothSignalRService.boothUpdates.subscribe(update => {
      console.log('BoothList: ✅ Received booth status update via SignalR:', update);
      // Refresh booth list to reflect changes
      console.log('BoothList: Reloading booth list due to status update...');
      this.loadBooths();
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
    this.booths = result.items.map(booth => ({
      ...booth,
      creationTime: booth.creationTime ? new Date(booth.creationTime) : null
    }));
    this.totalCount = result.totalCount;
    this.loading = false;
  },
  error: (error) => {
    this.messageService.add({
      severity: 'error',
      summary: this.localizationService.instant('::Messages:Error'),
      detail: this.localizationService.instant('::Booth:LoadError')
    });
    this.loading = false;
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

  showEditDialog(booth: BoothDto): void {
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

  deleteBooth(booth: BoothDto): void {
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

  changeBoothStatus(booth: BoothDto, newStatus: BoothStatus): void {
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

  openManualReservationDialog(booth: BoothDto): void {
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
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.localizationService.instant('::Messages:Error'),
          detail: 'Failed to load users'
        });
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

  trackByBoothId(index: number, booth: BoothDto): string {
    return booth.id;
  }
}