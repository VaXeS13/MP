import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService, ConfirmationService } from 'primeng/api';
import { FloorPlanService } from '../services/floor-plan.service';
import { TenantService } from '../services/tenant.service';
import { FloorPlanDto, GetFloorPlanListDto } from '../shared/models/floor-plan.model';

@Component({
  selector: 'app-floor-plan-list',
  standalone: false,
  template: `
    <div class="floor-plan-list p-4">
      <!-- Header -->
      <div class="flex justify-content-between align-items-center mb-4">
        <div>
          <h2 class="m-0">Plany Sali</h2>
          <p class="text-color-secondary m-0 mt-1">
            Zarządzaj planami sal dla swojego obiektu
          </p>
        </div>
        <p-button
          label="Nowy Plan"
          icon="pi pi-plus"
          (onClick)="createNew()">
        </p-button>
      </div>

      <!-- Filters -->
      <div class="filter-panel p-3 border-round surface-100 mb-4">
        <div class="grid align-items-end">
          <div class="col-12 md:col-4">
            <label class="block mb-2">Poziom</label>
            <p-dropdown
              [(ngModel)]="filterLevel"
              name="filterLevel"
              [options]="levelOptions"
              placeholder="Wszystkie poziomy"
              [showClear]="true"
              optionLabel="label"
              optionValue="value"
              (onChange)="onFilterChange()"
              class="w-full">
            </p-dropdown>
          </div>
          <div class="col-12 md:col-4">
            <label class="block mb-2">Status</label>
            <p-dropdown
              [(ngModel)]="filterStatus"
              name="filterStatus"
              [options]="statusOptions"
              placeholder="Wszystkie statusy"
              [showClear]="true"
              optionLabel="label"
              optionValue="value"
              (onChange)="onFilterChange()"
              class="w-full">
            </p-dropdown>
          </div>
          <div class="col-12 md:col-4">
            <label class="block mb-2">Szukaj</label>
            <p-inputText
              [(ngModel)]="filterText"
              name="filterText"
              placeholder="Nazwa planu..."
              (input)="onFilterChange()"
              class="w-full">
            </p-inputText>
          </div>
        </div>
      </div>

      <!-- Floor Plans Grid -->
      <div class="floor-plans-grid">
        <div *ngIf="loading" class="text-center p-8">
          <p-progressSpinner></p-progressSpinner>
          <div class="mt-3">Ładowanie planów...</div>
        </div>

        <div *ngIf="!loading && filteredFloorPlans.length === 0" class="text-center p-8">
          <i class="pi pi-map text-6xl text-400 mb-4"></i>
          <h3 class="text-color-secondary">Brak planów sali</h3>
          <p class="text-color-secondary mb-4">
            Nie znaleziono żadnych planów spełniających kryteria wyszukiwania.
          </p>
          <p-button
            label="Utwórz pierwszy plan"
            icon="pi pi-plus"
            (onClick)="createNew()">
          </p-button>
        </div>

        <div class="grid" *ngIf="!loading && filteredFloorPlans.length > 0">
          <div
            *ngFor="let floorPlan of filteredFloorPlans; trackBy: trackByFloorPlanId"
            class="col-12 md:col-6 lg:col-4">
            <div class="floor-plan-card p-4 border-round shadow-2 surface-card h-full">
              <!-- Card Header -->
              <div class="flex justify-content-between align-items-start mb-3">
                <div>
                  <h4 class="m-0 mb-1">{{ floorPlan.name }}</h4>
                  <div class="text-sm text-color-secondary">
                    <i class="pi pi-building mr-1"></i>
                    {{ getLevelDisplayName(floorPlan.level) }}
                  </div>
                </div>
                <p-badge
                  [value]="floorPlan.isActive ? 'Aktywny' : 'Nieaktywny'"
                  [severity]="floorPlan.isActive ? 'success' : 'secondary'">
                </p-badge>
              </div>

              <!-- Floor Plan Preview -->
              <div class="floor-plan-preview mb-3 border border-round overflow-hidden">
                <div class="preview-canvas bg-white p-2 text-center"
                     [style.height.px]="150"
                     [style.background-image]="'url(data:image/svg+xml;base64,' + generatePreviewSvg(floorPlan) + ')'"
                     [style.background-size]="'contain'"
                     [style.background-repeat]="'no-repeat'"
                     [style.background-position]="'center'">
                  <div *ngIf="floorPlan.booths.length === 0"
                       class="flex align-items-center justify-content-center h-full text-color-secondary">
                    <i class="pi pi-map text-4xl"></i>
                  </div>
                </div>
              </div>

              <!-- Floor Plan Stats -->
              <div class="floor-plan-stats mb-3">
                <div class="grid text-center">
                  <div class="col-4">
                    <div class="text-lg font-bold text-primary">{{ floorPlan.booths.length }}</div>
                    <div class="text-xs text-color-secondary">Stanowiska</div>
                  </div>
                  <div class="col-4">
                    <div class="text-lg font-bold text-primary">{{ floorPlan.width }}×{{ floorPlan.height }}</div>
                    <div class="text-xs text-color-secondary">Wymiary</div>
                  </div>
                  <div class="col-4">
                    <div class="text-lg font-bold text-primary">{{ getAvailableBoothsCount(floorPlan) }}</div>
                    <div class="text-xs text-color-secondary">Dostępne</div>
                  </div>
                </div>
              </div>

              <!-- Creation Info -->
              <div class="creation-info mb-3 text-sm text-color-secondary">
                <div class="flex justify-content-between">
                  <span>Utworzono:</span>
                  <span>{{ formatDate(floorPlan.creationTime) }}</span>
                </div>
                <div class="flex justify-content-between" *ngIf="floorPlan.lastModificationTime">
                  <span>Zmodyfikowano:</span>
                  <span>{{ formatDate(floorPlan.lastModificationTime) }}</span>
                </div>
              </div>

              <!-- Actions -->
              <div class="flex justify-content-between gap-2">
                <p-button
                  *ngIf="floorPlan.isActive"
                  label="Podgląd"
                  icon="pi pi-eye"
                  size="small"
                  [text]="true"
                  (onClick)="viewFloorPlan(floorPlan)">
                </p-button>
                <p-button
                  label="Edytuj"
                  icon="pi pi-pencil"
                  size="small"
                  [text]="true"
                  (onClick)="editFloorPlan(floorPlan)">
                </p-button>
                <p-button
                  *ngIf="!floorPlan.isActive"
                  label="Publikuj"
                  icon="pi pi-check"
                  size="small"
                  severity="success"
                  [text]="true"
                  (onClick)="publishFloorPlan(floorPlan)">
                </p-button>
                <p-button
                  *ngIf="floorPlan.isActive"
                  label="Dezaktywuj"
                  icon="pi pi-times"
                  size="small"
                  severity="warning"
                  [text]="true"
                  (onClick)="deactivateFloorPlan(floorPlan)">
                </p-button>
                <p-button
                  icon="pi pi-trash"
                  size="small"
                  severity="danger"
                  [text]="true"
                  (onClick)="deleteFloorPlan(floorPlan)"
                  pTooltip="Usuń plan">
                </p-button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirmation Dialog -->
    <p-confirmDialog></p-confirmDialog>
  `,
  styles: [`
    .floor-plan-card {
      transition: transform 0.2s, box-shadow 0.2s;
      border: 1px solid var(--surface-border);
    }

    .floor-plan-card:hover {
      transform: translateY(-2px);
      box-shadow: 0 8px 25px rgba(0,0,0,0.15);
    }

    .floor-plan-preview {
      background-color: #f8f9fa;
      min-height: 150px;
    }

    .preview-canvas {
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .floor-plans-grid {
      min-height: 400px;
    }

    .filter-panel {
      background: linear-gradient(135deg, var(--surface-50) 0%, var(--surface-100) 100%);
    }

    .creation-info {
      border-top: 1px solid var(--surface-border);
      padding-top: 0.75rem;
    }
  `]
})
export class FloorPlanListComponent implements OnInit {
  floorPlans: FloorPlanDto[] = [];
  filteredFloorPlans: FloorPlanDto[] = [];
  loading = false;

  // Filters
  filterLevel?: number;
  filterStatus?: boolean;
  filterText = '';

  levelOptions: any[] = [];
  statusOptions = [
    { label: 'Aktywne', value: true },
    { label: 'Nieaktywne', value: false }
  ];

  constructor(
    private floorPlanService: FloorPlanService,
    private tenantService: TenantService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadFloorPlans();
  }

  loadFloorPlans() {
    this.loading = true;

    const currentTenant = this.tenantService.getCurrentTenant();

    // Handle case where tenant is not available
    if (!currentTenant?.name) {
      this.loading = false;
      this.floorPlans = [];
      this.setupLevelOptions();
      this.applyFilters();
      this.messageService.add({
        severity: 'warn',
        summary: 'Uwaga',
        detail: 'Nie można określić aktualnego najemcy. Sprawdź konfigurację systemu.'
      });
      return;
    }

    // Use tenant ID if available, otherwise pass undefined to get all floor plans
    this.floorPlanService.getListByTenant(currentTenant.id || undefined).subscribe({
      next: (floorPlans) => {
        this.floorPlans = floorPlans;
        this.setupLevelOptions();
        this.applyFilters();
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Błąd',
          detail: 'Nie udało się pobrać planów sali'
        });
        console.error('Error loading floor plans:', error);
      }
    });
  }

  private setupLevelOptions() {
    const levels = [...new Set(this.floorPlans.map(fp => fp.level))].sort((a, b) => a - b);
    this.levelOptions = levels.map(level => ({
      label: this.getLevelDisplayName(level),
      value: level
    }));
  }

  onFilterChange() {
    this.applyFilters();
  }

  private applyFilters() {
    this.filteredFloorPlans = this.floorPlans.filter(fp => {
      const matchesLevel = this.filterLevel === undefined || fp.level === this.filterLevel;
      const matchesStatus = this.filterStatus === undefined || fp.isActive === this.filterStatus;
      const matchesText = !this.filterText ||
        fp.name.toLowerCase().includes(this.filterText.toLowerCase());

      return matchesLevel && matchesStatus && matchesText;
    });
  }

  getLevelDisplayName(level: number): string {
    return level === 0 ? 'Parter' : `${level}. Piętro`;
  }

  getAvailableBoothsCount(floorPlan: FloorPlanDto): number {
    return floorPlan.booths.filter(b => b.booth?.status === 1).length;
  }

  formatDate(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toLocaleDateString('pl-PL', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  generatePreviewSvg(floorPlan: FloorPlanDto): string {
    if (floorPlan.booths.length === 0) {
      return '';
    }

    const scale = Math.min(200 / floorPlan.width, 150 / floorPlan.height);
    const scaledWidth = floorPlan.width * scale;
    const scaledHeight = floorPlan.height * scale;

    let svg = `<svg width="${scaledWidth}" height="${scaledHeight}" xmlns="http://www.w3.org/2000/svg">`;
    svg += `<rect width="100%" height="100%" fill="#ffffff" stroke="#dee2e6" stroke-width="1"/>`;

    floorPlan.booths.forEach(booth => {
      const x = booth.x * scale;
      const y = booth.y * scale;
      const w = booth.width * scale;
      const h = booth.height * scale;
      const color = this.getBoothColor(booth.booth?.status);

      svg += `<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${color}" stroke="#333" stroke-width="0.5" rx="1"/>`;
    });

    svg += '</svg>';

    return btoa(svg);
  }

  private getBoothColor(status?: number): string {
    switch (status) {
      case 1: return '#4CAF50'; // Available
      case 2: return '#FF9800'; // Reserved
      case 3: return '#F44336'; // Rented
      case 4: return '#9E9E9E'; // Maintenance
      default: return '#2196F3';
    }
  }

  createNew() {
    this.router.navigate(['/floor-plans/editor']);
  }

  viewFloorPlan(floorPlan: FloorPlanDto) {
    this.router.navigate(['/floor-plans/view', floorPlan.id]);
  }

  editFloorPlan(floorPlan: FloorPlanDto) {
    this.router.navigate(['/floor-plans/editor', floorPlan.id]);
  }

  publishFloorPlan(floorPlan: FloorPlanDto) {
    this.confirmationService.confirm({
      message: `Czy na pewno chcesz opublikować plan "${floorPlan.name}"?`,
      header: 'Potwierdź publikację',
      icon: 'pi pi-question-circle',
      accept: () => {
        this.floorPlanService.publish(floorPlan.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Sukces',
              detail: 'Plan został opublikowany'
            });
            this.loadFloorPlans();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Błąd',
              detail: 'Nie udało się opublikować planu'
            });
          }
        });
      }
    });
  }

  deactivateFloorPlan(floorPlan: FloorPlanDto) {
    this.confirmationService.confirm({
      message: `Czy na pewno chcesz dezaktywować plan "${floorPlan.name}"?`,
      header: 'Potwierdź dezaktywację',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.floorPlanService.deactivate(floorPlan.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Sukces',
              detail: 'Plan został dezaktywowany'
            });
            this.loadFloorPlans();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Błąd',
              detail: 'Nie udało się dezaktywować planu'
            });
          }
        });
      }
    });
  }

  deleteFloorPlan(floorPlan: FloorPlanDto) {
    this.confirmationService.confirm({
      message: `Czy na pewno chcesz usunąć plan "${floorPlan.name}"? Ta akcja jest nieodwracalna.`,
      header: 'Potwierdź usunięcie',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.floorPlanService.delete(floorPlan.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Sukces',
              detail: 'Plan został usunięty'
            });
            this.loadFloorPlans();
          },
          error: () => {
            this.messageService.add({
              severity: 'error',
              summary: 'Błąd',
              detail: 'Nie udało się usunąć planu'
            });
          }
        });
      }
    });
  }

  trackByFloorPlanId(index: number, floorPlan: FloorPlanDto): string {
    return floorPlan.id;
  }
}