import { Component, OnInit } from '@angular/core';
import { MessageService, ConfirmationService } from 'primeng/api';
import { BoothTypeService } from '../../services/booth-type.service';
import { BoothTypeDto, GetBoothTypeListDto } from '../../shared/models/booth-type.model';
import { animate, state, style, transition, trigger } from '@angular/animations';
import { LocalizationService } from '@abp/ng.core';

@Component({
  standalone: false,
  selector: 'app-booth-type-list',
  templateUrl: './booth-type-list.component.html',
  styleUrls: ['./booth-type-list.component.scss'],
  animations: [
    trigger('slideInOut', [
      state('in', style({
        transform: 'translateY(0)',
        opacity: 1,
        'max-height': '200px',
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
export class BoothTypeListComponent implements OnInit {
  boothTypes: BoothTypeDto[] = [];
  totalCount = 0;
  loading = false;

  currentLocale: string = 'en-US';
  showAdvancedFilters = false;

  // Filters
  filterText = '';

  // Pagination
  first = 0;
  rows = 10;
  currentPage = 1;

  // Expose Math for template
  Math = Math;

  // Dialog
  displayCreateDialog = false;
  displayEditDialog = false;

  selectedBoothType: BoothTypeDto | null = null;

  constructor(
    private boothTypeService: BoothTypeService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private localizationService: LocalizationService
  ) {}

  ngOnInit(): void {
    this.currentLocale = this.localizationService.currentLang;
    this.loadBoothTypes();
  }

  toggleAdvancedFilters(): void {
    this.showAdvancedFilters = !this.showAdvancedFilters;
  }

  loadBoothTypes(): void {
    this.loading = true;

    const input: GetBoothTypeListDto = {
      filter: this.filterText || undefined,
      skipCount: this.first,
      maxResultCount: this.rows
    };

    this.boothTypeService.getList(input).subscribe({
      next: (result) => {
        this.boothTypes = result.items.map(boothType => ({
          ...boothType,
          creationTime: boothType.creationTime ? new Date(boothType.creationTime) : new Date()
        }));
        this.totalCount = result.totalCount;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.localizationService.instant('::Messages:Error'),
          detail: this.localizationService.instant('::BoothType:LoadError')
        });
        this.loading = false;
      }
    });
  }

  onFilter(): void {
    this.first = 0;
    this.loadBoothTypes();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.first = (page - 1) * this.rows;
    this.loadBoothTypes();
  }

  getStatusBadgeClass(isActive: boolean): string {
    return isActive ? 'badge bg-success' : 'badge bg-danger';
  }

  showCreateDialog(): void {
    this.displayCreateDialog = true;
  }

  showEditDialog(boothType: BoothTypeDto): void {
    this.selectedBoothType = boothType;
    this.displayEditDialog = true;
  }

  onBoothTypeCreated(): void {
    this.displayCreateDialog = false;
    this.loadBoothTypes();
    this.messageService.add({
      severity: 'success',
      summary: this.localizationService.instant('::Messages:Success'),
      detail: this.localizationService.instant('::BoothType:CreateSuccess')
    });
  }

  onBoothTypeUpdated(): void {
    this.displayEditDialog = false;
    this.selectedBoothType = null;
    this.loadBoothTypes();
    this.messageService.add({
      severity: 'success',
      summary: this.localizationService.instant('::Messages:Success'),
      detail: this.localizationService.instant('::BoothType:UpdateSuccess')
    });
  }

  deleteBoothType(boothType: BoothTypeDto): void {
    this.confirmationService.confirm({
      message: this.localizationService.instant('::BoothType:DeleteConfirmation', boothType.name),
      header: this.localizationService.instant('::Messages:Confirmation'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.boothTypeService.delete(boothType.id).subscribe({
          next: () => {
            this.loadBoothTypes();
            this.messageService.add({
              severity: 'success',
              summary: this.localizationService.instant('::Messages:Success'),
              detail: this.localizationService.instant('::BoothType:DeleteSuccess')
            });
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: this.localizationService.instant('::Messages:Error'),
              detail: this.localizationService.instant('::BoothType:DeleteError')
            });
          }
        });
      }
    });
  }

  toggleBoothTypeStatus(boothType: BoothTypeDto): void {
    const action = boothType.isActive ? 'deactivate' : 'activate';
    const serviceCall = boothType.isActive
      ? this.boothTypeService.deactivate(boothType.id)
      : this.boothTypeService.activate(boothType.id);

    serviceCall.subscribe({
      next: () => {
        this.loadBoothTypes();
        const successKey = boothType.isActive ? '::BoothType:DeactivateSuccess' : '::BoothType:ActivateSuccess';
        this.messageService.add({
          severity: 'success',
          summary: this.localizationService.instant('::Messages:Success'),
          detail: this.localizationService.instant(successKey)
        });
      },
      error: () => {
        const errorKey = boothType.isActive ? '::BoothType:DeactivateError' : '::BoothType:ActivateError';
        this.messageService.add({
          severity: 'error',
          summary: this.localizationService.instant('::Messages:Error'),
          detail: this.localizationService.instant(errorKey)
        });
      }
    });
  }

  getStatusSeverity(isActive: boolean): string {
    return isActive ? 'success' : 'danger';
  }

  getStatusLabel(isActive: boolean): string {
    return isActive
      ? this.localizationService.instant('::Common:Active')
      : this.localizationService.instant('::Common:Inactive');
  }
}