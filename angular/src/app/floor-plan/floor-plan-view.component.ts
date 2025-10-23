import {
  Component,
  OnInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  AfterViewInit,
  ChangeDetectorRef
} from '@angular/core';
import { Subscription, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { Router, ActivatedRoute } from '@angular/router';
import { Canvas, Rect, Text, Group, Shadow, FabricImage } from 'fabric';
import { ElementPosition } from '../shared/models/floor-plan.model';

// Local enum definition for FloorPlanElementType
enum FloorPlanElementType {
  Wall = 0,
  Door = 1,
  Window = 2,
  Pillar = 3,
  Checkout = 4,
  Restroom = 5,
  InfoDesk = 6,
  EmergencyExit = 7,
  Storage = 8,
  Stairs = 9,
  Zone = 10,
  TextLabel = 11
}
import { MessageService } from 'primeng/api';
import { FloorPlanService } from '../services/floor-plan.service';
import { TenantService } from '../services/tenant.service';
import { CartService } from '../services/cart.service';
import { BoothSignalRService } from '../services/booth-signalr.service';
import { FloorPlanDto, FloorPlanBoothDto, BoothAvailabilityDto } from '../shared/models/floor-plan.model';
import { BoothDto } from '../shared/models/booth.model';
import { CartDto, CartItemDto } from '../shared/models/cart.model';

@Component({
  selector: 'app-floor-plan-view',
  standalone: false,
  template: `
    <div class="floor-plan-view">
      <!-- Header -->
      <div class="header p-3 border-bottom bg-white">
        <div class="flex justify-content-between align-items-center">
          <div>
            <h2 class="m-0">Plan Sali - {{ currentTenant?.name || 'Domyślny' }}</h2>
            <p class="text-color-secondary m-0 mt-1">
              Kliknij na dostępne stanowisko (zielone) aby przejść do rezerwacji
            </p>
          </div>

          <!-- Level Selector -->
          <div class="flex align-items-center gap-3" *ngIf="availableLevels.length > 1">
            <label>Poziom:</label>
            <p-selectButton
              [options]="levelOptions"
              [(ngModel)]="selectedLevel"
              (onChange)="onLevelChange()"
              optionLabel="label"
              optionValue="value">
            </p-selectButton>
          </div>
        </div>

        <!-- Date Filter -->
        <div class="flex align-items-center gap-3 mt-3" *ngIf="currentFloorPlan">
          <label class="font-semibold">Filtruj dostępność:</label>
          <p-calendar
            [(ngModel)]="filterStartDate"
            [showIcon]="true"
            placeholder="Data od"
            dateFormat="yy-mm-dd"
            [minDate]="minDate">
          </p-calendar>
          <span>-</span>
          <p-calendar
            [(ngModel)]="filterEndDate"
            [showIcon]="true"
            placeholder="Data do"
            dateFormat="yy-mm-dd"
            [minDate]="filterStartDate || minDate">
          </p-calendar>
          <p-button
            label="Filtruj"
            icon="pi pi-filter"
            (onClick)="applyDateFilter()"
            [disabled]="!filterStartDate || !filterEndDate">
          </p-button>
          <p-button
            label="Wyczyść"
            icon="pi pi-times"
            [text]="true"
            (onClick)="clearDateFilter()"
            *ngIf="filterStartDate || filterEndDate">
          </p-button>
        </div>

        <!-- Floor Plan Info -->
        <div class="flex justify-content-between align-items-center mt-3" *ngIf="currentFloorPlan">
          <div class="flex align-items-center gap-4">
            <div class="flex align-items-center gap-2">
              <i class="pi pi-map text-primary"></i>
              <span><strong>{{ currentFloorPlan.name }}</strong></span>
            </div>
            <div class="flex align-items-center gap-2">
              <i class="pi pi-building text-primary"></i>
              <span>Poziom {{ currentFloorPlan.level }}</span>
            </div>
            <div class="flex align-items-center gap-2">
              <i class="pi pi-th-large text-primary"></i>
              <span>{{ currentFloorPlan.booths.length }} stanowisk</span>
            </div>
          </div>

          <!-- Zoom Controls -->
          <div class="flex align-items-center gap-2">
            <p-button
              label="Zmień plan"
              icon="pi pi-list"
              [text]="true"
              (onClick)="changeFloorPlan()"
              pTooltip="Wybierz inny plan">
            </p-button>

            <p-divider layout="vertical"></p-divider>

            <p-button
              icon="pi pi-search-minus"
              [text]="true"
              [rounded]="true"
              (onClick)="zoomOut()"
              pTooltip="Pomniejsz">
            </p-button>
            <span class="text-sm">{{ (currentZoom * 100).toFixed(0) }}%</span>
            <p-button
              icon="pi pi-search-plus"
              [text]="true"
              [rounded]="true"
              (onClick)="zoomIn()"
              pTooltip="Powiększ">
            </p-button>
            <p-button
              icon="pi pi-refresh"
              [text]="true"
              [rounded]="true"
              (onClick)="resetZoom()"
              pTooltip="Reset widoku">
            </p-button>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <div class="content-area flex" style="height: calc(100vh - 140px);">
        <!-- Floor Plan Selection Dialog -->
        <div *ngIf="!selectedFloorPlanId && floorPlans.length > 0" class="floor-plan-selection p-4">
          <div class="selection-content bg-white border-round shadow-2 p-4">
            <h3 class="text-center mb-4">Wybierz Plan Sali</h3>
            <p class="text-center text-color-secondary mb-4">
              Wybierz plan sali, który chcesz wyświetlić
            </p>
            <div class="grid">
              <div *ngFor="let plan of floorPlans; trackBy: trackByFloorPlanId" [ngClass]="{'col-12': floorPlans.length === 1, 'col-12 md:col-6': floorPlans.length > 1}">
                <div
                  class="floor-plan-card p-3 border-round border-1 surface-border cursor-pointer"
                  (click)="selectFloorPlan(plan.id)"
                  style="transition: all 0.2s;">
                  <div class="flex align-items-center gap-3">
                    <div class="floor-plan-icon bg-primary-100 text-primary border-round p-3">
                      <i class="pi pi-map text-2xl"></i>
                    </div>
                    <div class="flex-1">
                      <div class="font-semibold">{{ plan.name }}</div>
                      <div class="text-sm text-color-secondary">
                        Poziom: {{ plan.level === 0 ? 'Parter' : plan.level }}
                      </div>
                      <div class="text-sm text-color-secondary">
                        Stanowisk: {{ plan.booths.length }}
                      </div>
                    </div>
                    <i class="pi pi-chevron-right text-color-secondary"></i>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Legend Panel -->
        <div *ngIf="selectedFloorPlanId" class="legend-panel p-3 bg-white border-right" style="width: 250px;">
          <h4>Legenda</h4>
          <div class="legend-items">
            <div class="legend-item flex align-items-center justify-content-between gap-2 mb-2">
              <div class="flex align-items-center gap-2">
                <div class="legend-color" style="background-color: #4CAF50;"></div>
                <span>Dostępne</span>
              </div>
              <p-badge [value]="getBoothCountByStatus('available').toString()" severity="success"></p-badge>
            </div>
            <div class="legend-item flex align-items-center justify-content-between gap-2 mb-2">
              <div class="flex align-items-center gap-2">
                <div class="legend-color" style="background-color: #FF9800;"></div>
                <span>Zarezerwowane</span>
              </div>
              <p-badge [value]="getBoothCountByStatus('reserved').toString()" severity="warning"></p-badge>
            </div>
            <div class="legend-item flex align-items-center justify-content-between gap-2 mb-2">
              <div class="flex align-items-center gap-2">
                <div class="legend-color" style="background-color: #F44336;"></div>
                <span>Wynajęte</span>
              </div>
              <p-badge [value]="getBoothCountByStatus('rented').toString()" severity="danger"></p-badge>
            </div>
            <div class="legend-item flex align-items-center justify-content-between gap-2 mb-2">
              <div class="flex align-items-center gap-2">
                <div class="legend-color" style="background-color: #9E9E9E;"></div>
                <span>Konserwacja</span>
              </div>
              <p-badge [value]="getBoothCountByStatus('maintenance').toString()" [severity]="'secondary'"></p-badge>
            </div>
            <div class="legend-item flex align-items-center justify-content-between gap-2 mb-2 mt-3 pt-3 border-top-1 surface-border">
              <div class="flex align-items-center gap-2">
                <span style="font-size: 1.2em;">🛒</span>
                <span>W koszyku</span>
              </div>
              <p-badge [value]="getBoothsInCartCount().toString()" severity="info"></p-badge>
            </div>
          </div>

          <!-- Date Filter Info -->
          <div *ngIf="filterStartDate && filterEndDate" class="mt-4 p-3 border-round surface-100">
            <h5 class="mt-0 mb-2">Filtr aktywny</h5>
            <div class="text-sm">
              <div>Od: <strong>{{ filterStartDate | date:'dd.MM.yyyy' }}</strong></div>
              <div>Do: <strong>{{ filterEndDate | date:'dd.MM.yyyy' }}</strong></div>
            </div>
          </div>
        </div>

        <!-- Canvas Area -->
        <div *ngIf="selectedFloorPlanId" class="canvas-container flex-1 p-3 bg-gray-50">
          <div class="canvas-wrapper" *ngIf="currentFloorPlan; else noFloorPlan">
            <canvas
              #fabricCanvas
              [width]="currentFloorPlan.width"
              [height]="currentFloorPlan.height"
              class="floor-plan-canvas">
            </canvas>
          </div>

          <!-- No Floor Plan Message -->
          <ng-template #noFloorPlan>
            <div class="no-floor-plan text-center p-8">
              <i class="pi pi-map text-6xl text-400 mb-4"></i>
              <h3 class="text-color-secondary">Brak dostępnego planu sali</h3>
              <p class="text-color-secondary">
                Nie znaleziono aktywnego planu sali dla poziomu {{ selectedLevel }}.
              </p>
              <p-button
                label="Odśwież"
                icon="pi pi-refresh"
                (onClick)="loadFloorPlans()">
              </p-button>
            </div>
          </ng-template>
        </div>
      </div>

      <!-- Loading Overlay -->
      <div *ngIf="loading" class="loading-overlay">
        <div class="loading-content text-center">
          <p-progressSpinner></p-progressSpinner>
          <div class="mt-3">Ładowanie planu sali...</div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .floor-plan-view {
      height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .header {
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
      z-index: 10;
    }

    .legend-color {
      width: 20px;
      height: 20px;
      border-radius: 4px;
      border: 1px solid #ddd;
    }

    .floor-plan-canvas {
      border: 1px solid #dee2e6;
      background: white;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
      border-radius: 8px;
      cursor: pointer;
    }

    .canvas-container {
      overflow: auto;
      position: relative;
    }

    .canvas-wrapper {
      display: flex;
      justify-content: center;
      align-items: flex-start;
      min-height: 100%;
      padding: 20px;
    }

    .selected-booth-info {
      border-left: 4px solid var(--primary-500);
    }

    .no-floor-plan {
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      height: 100%;
    }

    .loading-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.8);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 1000;
    }

    .loading-content {
      background: white;
      padding: 2rem;
      border-radius: 8px;
      box-shadow: 0 4px 6px rgba(0,0,0,0.1);
    }

    .booth-hover {
      filter: brightness(1.1);
      transform: scale(1.02);
      transition: all 0.2s ease;
    }

    .floor-plan-selection {
      width: 100%;
      height: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f8f9fa;
    }

    .selection-content {
      max-width: 800px;
      width: 100%;
      margin: 0 auto;
    }

    .floor-plan-card:hover {
      background: var(--surface-hover);
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }

    .floor-plan-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 50px;
      height: 50px;
    }
  `]
})
export class FloorPlanViewComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('fabricCanvas', { static: false }) canvasElement!: ElementRef<HTMLCanvasElement>;

  canvas!: Canvas;
  floorPlans: FloorPlanDto[] = [];
  currentFloorPlan?: FloorPlanDto;
  selectedBooth?: BoothDto;
  currentTenant: any;
  selectedFloorPlanId?: string;

  // Level management
  availableLevels: number[] = [];
  selectedLevel = 0;
  levelOptions: { label: string; value: number }[] = [];

  // Canvas properties
  currentZoom = 1;
  minZoom = 0.2;
  maxZoom = 3;

  // Date filter
  filterStartDate?: Date;
  filterEndDate?: Date;
  minDate = new Date();
  boothAvailability: BoothAvailabilityDto[] = [];

  // Cart state
  currentCart: CartDto | null = null;
  private cartSubscription?: Subscription;
  private boothUpdatesSubscription?: Subscription;
  private currentFloorPlanSubscription?: Subscription;

  // State
  loading = false;
  private viewInitialized = false;
  private destroy$ = new Subject<void>();

  constructor(
    private floorPlanService: FloorPlanService,
    private tenantService: TenantService,
    private cartService: CartService,
    private boothSignalRService: BoothSignalRService,
    private messageService: MessageService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    // Set default filter dates: today to today + 7 days
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const nextWeek = new Date(today);
    nextWeek.setDate(nextWeek.getDate() + 7);

    this.filterStartDate = today;
    this.filterEndDate = nextWeek;

    this.loadCurrentTenant();

    // Subscribe to cart changes
    this.cartSubscription = this.cartService.cart$.subscribe(cart => {
      this.currentCart = cart;
      // Re-render booths when cart changes if canvas is initialized
      if (this.canvas && this.currentFloorPlan) {
        this.canvas.clear();
        this.renderElements();
        this.renderBooths();
      }
    });

    // Subscribe to booth status updates via SignalR (all booth changes in tenant)
    console.log('FloorPlanView: Setting up booth updates subscription...');
    this.boothUpdatesSubscription = this.boothSignalRService.boothUpdates
      .pipe(takeUntil(this.destroy$))
      .subscribe(update => {
        console.log('FloorPlanView: ✅ Received booth status update via SignalR:', update);
        console.log('FloorPlanView: Current state - selectedFloorPlanId:', this.selectedFloorPlanId, 'currentFloorPlan:', this.currentFloorPlan?.name);

        // Refresh floor plan to reflect booth status changes
        if (this.selectedFloorPlanId && this.currentFloorPlan) {
          console.log('FloorPlanView: Refreshing floor plan due to booth update...');
          // Re-fetch floor plan data with updated booth availability
          this.selectFloorPlan(this.selectedFloorPlanId);
        } else {
          console.warn('FloorPlanView: Cannot refresh - no floor plan selected');
        }
      });
    console.log('FloorPlanView: ✅ Booth updates subscription active');

    // Check if we have a floor plan ID in the route
    const floorPlanId = this.route.snapshot.paramMap.get('id');
    if (floorPlanId) {
      this.selectedFloorPlanId = floorPlanId;
      this.loadFloorPlans();
    } else {
      this.loadFloorPlans();
    }
  }

  ngAfterViewInit() {
    console.log('ngAfterViewInit called, currentFloorPlan:', this.currentFloorPlan);
    this.viewInitialized = true;

    // If data already loaded, initialize canvas now
    if (this.currentFloorPlan) {
      setTimeout(() => {
        this.initializeFabricCanvas();
      });
    }
  }

  ngOnDestroy() {
    // Unsubscribe from floor plan updates
    this.unsubscribeFromFloorPlanUpdates();

    // Clean up all subscriptions
    this.destroy$.next();
    this.destroy$.complete();

    // Clean up cart subscription
    if (this.cartSubscription) {
      this.cartSubscription.unsubscribe();
    }

    // Clean up booth updates subscription
    if (this.boothUpdatesSubscription) {
      this.boothUpdatesSubscription.unsubscribe();
    }

    if (this.canvas) {
      this.canvas.dispose();
    }
    if (this.cartSubscription) {
      this.cartSubscription.unsubscribe();
    }
    if (this.boothUpdatesSubscription) {
      this.boothUpdatesSubscription.unsubscribe();
    }
  }

  private loadCurrentTenant() {
    this.currentTenant = this.tenantService.getCurrentTenant();
  }

  loadFloorPlans() {
    this.loading = true;

    this.floorPlanService.getListByTenant(this.currentTenant?.id, true).subscribe({
      next: (floorPlans) => {
        console.log('Loaded floor plans:', floorPlans);
        this.floorPlans = floorPlans;
        this.setupLevelOptions();
        this.selectDefaultLevel();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading floor plans:', error);
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Błąd',
          detail: 'Nie udało się pobrać planów sali'
        });
      }
    });
  }

  private setupLevelOptions() {
    this.availableLevels = this.floorPlanService.getAvailableLevels(this.floorPlans);
    this.levelOptions = this.availableLevels.map(level => ({
      label: level === 0 ? 'Parter' : `${level}. Piętro`,
      value: level
    }));
  }

  private selectDefaultLevel() {
    // If we have a pre-selected floor plan ID, use it
    if (this.selectedFloorPlanId) {
      const plan = this.floorPlans.find(p => p.id === this.selectedFloorPlanId);
      if (plan) {
        this.selectedLevel = plan.level;
        this.updateCurrentFloorPlan();
      }
    }
    // Otherwise don't auto-select - let user choose
    else if (this.availableLevels.length > 0) {
      this.selectedLevel = this.availableLevels[0];
    }
  }

  selectFloorPlan(floorPlanId: string) {
    this.selectedFloorPlanId = floorPlanId;
    const plan = this.floorPlans.find(p => p.id === floorPlanId);
    if (plan) {
      this.selectedLevel = plan.level;
      this.updateCurrentFloorPlan();

      // Subscribe to updates specific to this floor plan
      this.subscribeToFloorPlanUpdates(floorPlanId);
    }
  }

  changeFloorPlan() {
    // Unsubscribe from current floor plan updates
    this.unsubscribeFromFloorPlanUpdates();

    this.selectedFloorPlanId = undefined;
    this.currentFloorPlan = undefined;
    this.selectedBooth = undefined;
    if (this.canvas) {
      this.canvas.dispose();
    }
  }

  /**
   * Subscribe to SignalR updates for a specific floor plan
   */
  private async subscribeToFloorPlanUpdates(floorPlanId: string): Promise<void> {
    // Unsubscribe from previous floor plan if any
    this.unsubscribeFromFloorPlanUpdates();

    try {
      console.log(`FloorPlanView: Subscribing to floor plan updates for: ${floorPlanId}`);
      await this.boothSignalRService.subscribeToFloorPlan(floorPlanId);
      console.log(`FloorPlanView: ✅ Subscribed to floor plan: ${floorPlanId}`);
    } catch (error) {
      console.error('FloorPlanView: Failed to subscribe to floor plan updates', error);
    }
  }

  /**
   * Unsubscribe from current floor plan updates
   */
  private async unsubscribeFromFloorPlanUpdates(): Promise<void> {
    if (this.selectedFloorPlanId) {
      try {
        console.log(`FloorPlanView: Unsubscribing from floor plan updates for: ${this.selectedFloorPlanId}`);
        await this.boothSignalRService.unsubscribeFromFloorPlan(this.selectedFloorPlanId);
        console.log(`FloorPlanView: ✅ Unsubscribed from floor plan: ${this.selectedFloorPlanId}`);
      } catch (error) {
        console.error('FloorPlanView: Failed to unsubscribe from floor plan updates', error);
      }
    }
  }

  onLevelChange() {
    this.updateCurrentFloorPlan();
  }

  private updateCurrentFloorPlan() {
    this.currentFloorPlan = this.floorPlanService.getActiveFloorPlan(this.floorPlans, this.selectedLevel);
    console.log('Current floor plan:', this.currentFloorPlan);
    console.log('View initialized:', this.viewInitialized);
    console.log('Canvas element:', this.canvasElement);
    this.selectedBooth = undefined;

    if (this.canvas) {
      this.canvas.dispose();
    }

    // Only initialize if view is ready
    if (this.currentFloorPlan && this.viewInitialized) {
      setTimeout(() => {
        this.initializeFabricCanvas();

        // Auto-apply date filter if dates are set
        if (this.filterStartDate && this.filterEndDate) {
          setTimeout(() => {
            this.applyDateFilter();
          }, 100);
        }
      }, 0);
    }
  }

  private initializeFabricCanvas() {
    if (!this.currentFloorPlan || !this.canvasElement) {
      console.log('Cannot initialize canvas:', {
        hasFloorPlan: !!this.currentFloorPlan,
        hasCanvasElement: !!this.canvasElement
      });
      return;
    }

    console.log('Initializing canvas with dimensions:', this.currentFloorPlan.width, this.currentFloorPlan.height);
    console.log('Booths to render:', this.currentFloorPlan.booths);

    this.canvas = new Canvas(this.canvasElement.nativeElement, {
      width: this.currentFloorPlan.width,
      height: this.currentFloorPlan.height,
      backgroundColor: '#ffffff',
      selection: false,
      interactive: true
    });

    this.renderElements();
    this.renderBooths();
    this.setupCanvasInteractions();
  }

  private renderElements() {
    if (!this.currentFloorPlan || !this.canvas) {
      return;
    }

    if (this.currentFloorPlan.elements && this.currentFloorPlan.elements.length > 0) {
      this.currentFloorPlan.elements.forEach(element => {
        const elementPosition: ElementPosition = {
          id: element.id,
          elementType: element.elementType!,
          x: element.x,
          y: element.y,
          width: element.width,
          height: element.height,
          rotation: element.rotation,
          color: element.color,
          text: element.text,
          iconName: element.iconName,
          thickness: element.thickness,
          opacity: element.opacity,
          direction: element.direction
        };
        this.renderElementOnCanvas(elementPosition);
      });
    }
  }

  private async renderElementOnCanvas(element: ElementPosition) {
    const svgString = this.getElementSvg(element.elementType, element.width, element.height, element.color);

    if (svgString && element.elementType !== FloorPlanElementType.TextLabel) {
      const svgUrl = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svgString);

      try {
        const img = await FabricImage.fromURL(svgUrl);
        img.set({
          left: element.x,
          top: element.y,
          scaleX: element.width / (img.width || 1),
          scaleY: element.height / (img.height || 1),
          angle: element.rotation,
          opacity: element.opacity || 1,
          selectable: false,
          evented: false
        });
        this.canvas.add(img);
        this.canvas.renderAll();
      } catch (error) {
        console.error('Error loading SVG:', error);
        this.renderSimpleElement(element);
      }
    } else {
      this.renderSimpleElement(element);
    }
  }

  private renderSimpleElement(element: ElementPosition) {
    const rect = new Rect({
      left: element.x,
      top: element.y,
      width: element.width,
      height: element.height,
      fill: element.color || '#ccc',
      stroke: '#333',
      strokeWidth: element.elementType === FloorPlanElementType.Zone ? 1 : 2,
      angle: element.rotation,
      opacity: element.opacity || 1,
      selectable: false,
      evented: false
    });

    if (element.elementType === FloorPlanElementType.TextLabel) {
      const text = new Text(element.text || 'Text', {
        left: element.x + element.width / 2,
        top: element.y + element.height / 2,
        fontSize: 14,
        fontFamily: 'Arial',
        fill: '#000',
        textAlign: 'center',
        originX: 'center',
        originY: 'center',
        selectable: false,
        evented: false
      });

      const group = new Group([rect, text], {
        left: element.x,
        top: element.y,
        selectable: false,
        evented: false
      });

      this.canvas.add(group);
    } else {
      this.canvas.add(rect);
    }
    this.canvas.renderAll();
  }

  private getElementSvg(elementType: number, width: number, height: number, color?: string): string {
    const fillColor = color || '#ccc';
    const w = width;
    const h = height;

    switch (elementType) {
      case FloorPlanElementType.Wall:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2"/>
        </svg>`;

      case FloorPlanElementType.Door:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2"/>
          <path d="M 5 ${h/2} L ${w-5} ${h/2}" stroke="#fff" stroke-width="3"/>
          <circle cx="${w/2}" cy="${h/2}" r="4" fill="#fff"/>
        </svg>`;

      case FloorPlanElementType.Window:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2"/>
          <line x1="${w/2}" y1="0" x2="${w/2}" y2="${h}" stroke="#fff" stroke-width="2"/>
          <line x1="0" y1="${h/2}" x2="${w}" y2="${h/2}" stroke="#fff" stroke-width="2"/>
        </svg>`;

      case FloorPlanElementType.Pillar:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <circle cx="${w/2}" cy="${h/2}" r="${Math.min(w, h)/2 - 2}" fill="${fillColor}" stroke="#333" stroke-width="2"/>
          <circle cx="${w/2}" cy="${h/2}" r="${Math.min(w, h)/3}" fill="#555"/>
        </svg>`;

      case FloorPlanElementType.Checkout:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2" rx="4"/>
          <rect x="${w*0.1}" y="${h*0.2}" width="${w*0.8}" height="${h*0.25}" fill="#fff" rx="2"/>
          <rect x="${w*0.15}" y="${h*0.55}" width="${w*0.3}" height="${h*0.3}" fill="#2e7d32" rx="2"/>
          <rect x="${w*0.55}" y="${h*0.55}" width="${w*0.3}" height="${h*0.3}" fill="#2e7d32" rx="2"/>
          <text x="${w/2}" y="${h*0.35}" text-anchor="middle" font-size="${h/5}" fill="#333">$</text>
        </svg>`;

      case FloorPlanElementType.Restroom:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2" rx="4"/>
          <circle cx="${w*0.35}" cy="${h*0.3}" r="${h/8}" fill="#fff"/>
          <rect x="${w*0.3}" y="${h*0.45}" width="${w*0.1}" height="${h*0.35}" fill="#fff" rx="1"/>
          <circle cx="${w*0.65}" cy="${h*0.3}" r="${h/8}" fill="#fff"/>
          <polygon points="${w*0.65},${h*0.45} ${w*0.6},${h*0.8} ${w*0.7},${h*0.8}" fill="#fff"/>
          <text x="${w/2}" y="${h*0.95}" text-anchor="middle" font-size="${h/8}" fill="#fff">WC</text>
        </svg>`;

      case FloorPlanElementType.InfoDesk:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2" rx="4"/>
          <circle cx="${w/2}" cy="${h*0.45}" r="${h/4}" fill="#fff" stroke="#fff" stroke-width="3"/>
          <text x="${w/2}" y="${h*0.57}" text-anchor="middle" font-size="${h/2.5}" font-weight="bold" fill="${fillColor}">i</text>
        </svg>`;

      case FloorPlanElementType.EmergencyExit:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2" rx="4"/>
          <rect x="${w*0.1}" y="${h*0.2}" width="${w*0.35}" height="${h*0.6}" fill="#fff" rx="2"/>
          <rect x="${w*0.12}" y="${h*0.25}" width="${w*0.31}" height="${h*0.5}" fill="${fillColor}"/>
          <circle cx="${w*0.65}" cy="${h*0.3}" r="${h/8}" fill="#fff"/>
          <line x1="${w*0.65}" y1="${h*0.4}" x2="${w*0.65}" y2="${h*0.6}" stroke="#fff" stroke-width="3"/>
          <line x1="${w*0.65}" y1="${h*0.6}" x2="${w*0.55}" y2="${h*0.75}" stroke="#fff" stroke-width="3"/>
          <line x1="${w*0.65}" y1="${h*0.45}" x2="${w*0.8}" y2="${h*0.5}" stroke="#fff" stroke-width="3"/>
          <polygon points="${w*0.8},${h*0.45} ${w*0.9},${h*0.5} ${w*0.8},${h*0.55}" fill="#fff"/>
        </svg>`;

      case FloorPlanElementType.Storage:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2" rx="4"/>
          <rect x="${w*0.2}" y="${h*0.25}" width="${w*0.6}" height="${h*0.5}" fill="#fff" stroke="#666" stroke-width="2"/>
          <line x1="${w*0.2}" y1="${h*0.5}" x2="${w*0.8}" y2="${h*0.5}" stroke="#666" stroke-width="2"/>
          <line x1="${w/2}" y1="${h*0.25}" x2="${w/2}" y2="${h*0.75}" stroke="#666" stroke-width="2"/>
          <rect x="${w*0.45}" y="${h*0.47}" width="${w*0.1}" height="${h*0.06}" fill="#666"/>
        </svg>`;

      case FloorPlanElementType.Stairs:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#333" stroke-width="2"/>
          ${Array.from({length: 5}, (_, i) =>
            `<line x1="0" y1="${(i+1)*h/6}" x2="${w}" y2="${(i+1)*h/6}" stroke="#333" stroke-width="2"/>`
          ).join('')}
        </svg>`;

      case FloorPlanElementType.Zone:
        return `<svg width="${w}" height="${h}" xmlns="http://www.w3.org/2000/svg">
          <rect width="${w}" height="${h}" fill="${fillColor}" stroke="#666" stroke-width="1" stroke-dasharray="5,5" rx="8"/>
        </svg>`;

      default:
        return '';
    }
  }

  private renderBooths() {
    if (!this.currentFloorPlan || !this.canvas) {
      console.log('Cannot render booths:', {
        hasFloorPlan: !!this.currentFloorPlan,
        hasCanvas: !!this.canvas
      });
      return;
    }

    console.log('Rendering', this.currentFloorPlan.booths.length, 'booths');

    this.currentFloorPlan.booths.forEach((boothData, index) => {
      console.log(`Rendering booth ${index}:`, boothData);
      this.renderBooth(boothData);
    });

    this.canvas.renderAll();
    console.log('Canvas objects after render:', this.canvas.getObjects().length);
  }

  private renderBooth(boothData: FloorPlanBoothDto) {
    // Find availability data for this booth if filter is active
    const availability = this.boothAvailability.find(a => a.boothId === boothData.boothId);

    // Check if booth is in cart
    const cartItems = this.getCartItemsForBooth(boothData.boothId);
    const isInCart = cartItems.length > 0;

    // Determine color based on availability or booth status
    let fillColor: string;
    if (availability) {
      fillColor = this.getBoothColor(availability.status);
    } else {
      fillColor = this.getBoothColorFromNumber(boothData.booth?.status);
    }

    // Use special stroke color for booths in cart
    const strokeColor = isInCart ? '#2196F3' : '#333';
    const strokeWidth = isInCart ? 3 : 2;

    const rect = new Rect({
      left: 0,
      top: 0,
      width: boothData.width,
      height: boothData.height,
      fill: fillColor,
      stroke: strokeColor,
      strokeWidth: strokeWidth,
      rx: 4,
      ry: 4,
      selectable: false,
      hoverCursor: 'pointer',
      moveCursor: 'pointer'
    });

    // Calculate responsive font sizes
    const numberFontSize = Math.min(boothData.width / 3.5, 18);
    const dateFontSize = Math.min(boothData.width / 8, 11);
    const iconSize = Math.min(boothData.width / 6, 12);

    const boothNumber = new Text(boothData.booth?.number || '', {
      left: boothData.width / 2,
      top: boothData.height * 0.35,
      fontSize: numberFontSize,
      fontFamily: 'Arial, sans-serif',
      fill: '#fff',
      textAlign: 'center',
      originX: 'center',
      originY: 'center',
      selectable: false,
      evented: false,
      fontWeight: 'bold'
    });

    const elements: any[] = [rect, boothNumber];

    // Add cart icon and info if booth is in cart
    if (isInCart && cartItems.length > 0) {
      const cartItem = cartItems[0]; // Show first cart item info

      // Cart badge in top-right corner
      const badgeSize = Math.min(boothData.width * 0.15, 20);
      const cartBadge = new Rect({
        left: boothData.width - badgeSize - 5,
        top: 5,
        width: badgeSize,
        height: badgeSize,
        fill: '#2196F3',
        rx: badgeSize / 4,
        ry: badgeSize / 4,
        selectable: false,
        evented: false,
        stroke: '#fff',
        strokeWidth: 1.5
      });
      elements.push(cartBadge);

      // Simple cart icon using Unicode (shopping bag symbol)
      const cartIcon = new Text('⛁', {
        left: boothData.width - badgeSize / 2 - 5,
        top: badgeSize / 2 + 5,
        fontSize: iconSize,
        fontFamily: 'Arial, sans-serif',
        fill: '#fff',
        originX: 'center',
        originY: 'center',
        selectable: false,
        evented: false,
        fontWeight: 'bold'
      });
      elements.push(cartIcon);

      // Date range text below booth number
      const startDate = new Date(cartItem.startDate);
      const endDate = new Date(cartItem.endDate);
      const dateRangeText = `${startDate.toLocaleDateString('pl-PL', { day: '2-digit', month: '2-digit' })} - ${endDate.toLocaleDateString('pl-PL', { day: '2-digit', month: '2-digit' })}`;

      // Background for cart date
      const cartDateBg = new Rect({
        left: boothData.width / 2,
        top: boothData.height * 0.6,
        width: boothData.width * 0.85,
        height: dateFontSize + 6,
        fill: 'rgba(33, 150, 243, 0.95)',
        rx: 3,
        ry: 3,
        originX: 'center',
        originY: 'center',
        selectable: false,
        evented: false
      });
      elements.push(cartDateBg);

      const cartDateText = new Text(dateRangeText, {
        left: boothData.width / 2,
        top: boothData.height * 0.6,
        fontSize: dateFontSize,
        fontFamily: 'Arial, sans-serif',
        fill: '#fff',
        textAlign: 'center',
        originX: 'center',
        originY: 'center',
        selectable: false,
        evented: false,
        fontWeight: '500'
      });
      elements.push(cartDateText);

      // Show additional items count badge if more than one
      if (cartItems.length > 1) {
        const countBadge = new Rect({
          left: boothData.width - badgeSize - 3,
          top: 3,
          width: badgeSize * 0.6,
          height: badgeSize * 0.6,
          fill: '#FF5722',
          rx: badgeSize * 0.3,
          ry: badgeSize * 0.3,
          selectable: false,
          evented: false,
          stroke: '#fff',
          strokeWidth: 1.5
        });
        elements.push(countBadge);

        const countText = new Text(`${cartItems.length}`, {
          left: boothData.width - badgeSize * 0.7 - 3,
          top: badgeSize * 0.3 + 3,
          fontSize: iconSize * 0.7,
          fontFamily: 'Arial, sans-serif',
          fill: '#fff',
          textAlign: 'center',
          originX: 'center',
          originY: 'center',
          selectable: false,
          evented: false,
          fontWeight: 'bold'
        });
        elements.push(countText);
      }
    }

    // Add NextAvailableFrom text if availability data exists
    if (availability) {
      const nextAvailableDate = new Date(availability.nextAvailableFrom);
      const dateText = nextAvailableDate.toLocaleDateString('pl-PL', {
        day: '2-digit',
        month: '2-digit'
      });

      // Position availability date differently if cart info is present
      const availTop = isInCart ? boothData.height * 0.8 : boothData.height * 0.65;

      // Background for availability date
      const availBg = new Rect({
        left: boothData.width / 2,
        top: availTop,
        width: boothData.width * 0.7,
        height: dateFontSize + 4,
        fill: 'rgba(0, 0, 0, 0.3)',
        rx: 2,
        ry: 2,
        originX: 'center',
        originY: 'center',
        selectable: false,
        evented: false
      });
      elements.push(availBg);

      const availabilityText = new Text(`Od: ${dateText}`, {
        left: boothData.width / 2,
        top: availTop,
        fontSize: dateFontSize * 0.95,
        fontFamily: 'Arial, sans-serif',
        fill: '#fff',
        textAlign: 'center',
        originX: 'center',
        originY: 'center',
        selectable: false,
        evented: false
      });

      elements.push(availabilityText);
    }

    const group = new Group(elements, {
      left: boothData.x,
      top: boothData.y,
      angle: boothData.rotation,
      selectable: false,
      hoverCursor: 'pointer',
      moveCursor: 'pointer'
    });

    (group as any).boothData = boothData;
    (group as any).booth = boothData.booth;
    (group as any).availability = availability;

    this.canvas.add(group);
  }

  private setupCanvasInteractions() {
    this.canvas.on('mouse:over', (e) => {
      if (e.target && (e.target as any).booth) {
        const booth = (e.target as any).booth as BoothDto;
        const availability = (e.target as any).availability as BoothAvailabilityDto | undefined;

        // Add shadow for all booths - green for available, default for others
        const isAvailable = availability ? availability.status === 'available' : booth.status === 1;
        e.target.set({
          shadow: new Shadow({
            color: isAvailable ? 'rgba(76, 175, 80, 0.4)' : 'rgba(0,0,0,0.3)',
            blur: 10,
            offsetX: 5,
            offsetY: 5
          })
        });

        // All booths are clickable now
        this.canvas.hoverCursor = 'pointer';

        // Show availability info in title attribute (for browser tooltip)
        if (availability) {
          const statusText = this.getStatusDisplayText(availability.status);
          const nextAvailable = new Date(availability.nextAvailableFrom).toLocaleDateString('pl-PL');
          this.canvas.upperCanvasEl.title = `${booth.number}\nStatus: ${statusText}\nDostępne od: ${nextAvailable}`;
        } else {
          const statusText = this.getStatusDisplayTextFromNumber(booth.status);
          this.canvas.upperCanvasEl.title = `${booth.number}\nStatus: ${statusText}`;
        }

        this.canvas.renderAll();
      }
    });

    this.canvas.on('mouse:out', (e) => {
      if (e.target && (e.target as any).booth) {
        e.target.set({ shadow: null });
        this.canvas.hoverCursor = 'default';
        this.canvas.upperCanvasEl.title = '';
        this.canvas.renderAll();
      }
    });

    this.canvas.on('mouse:down', (e) => {
      if (e.target && (e.target as any).booth) {
        this.selectBooth((e.target as any).booth);
      } else {
        this.selectedBooth = undefined;
      }
    });
  }

  private getStatusDisplayText(status: string): string {
    switch (status) {
      case 'available': return 'Dostępne';
      case 'reserved': return 'Zarezerwowane';
      case 'rented': return 'Wynajęte';
      case 'maintenance': return 'Konserwacja';
      default: return 'Nieznany';
    }
  }

  private getStatusDisplayTextFromNumber(status: number): string {
    switch (status) {
      case 1: return 'Dostępne';
      case 2: return 'Zarezerwowane';
      case 3: return 'Wynajęte';
      case 4: return 'Konserwacja';
      default: return 'Nieznany';
    }
  }

  private selectBooth(booth: BoothDto) {
    this.selectedBooth = booth;

    // Navigate to booking page
    this.navigateToBoothBooking(booth);
  }

  private navigateToBoothBooking(booth: BoothDto) {
    // Navigate to rentals booking page with booth ID (for all statuses now)
    this.router.navigate(['/rentals/book', booth.id]);
  }

  private getBoothColor(status?: string): string {
    switch (status) {
      case 'available': return '#4CAF50'; // Available - Green
      case 'reserved': return '#FF9800'; // Reserved - Orange
      case 'rented': return '#F44336'; // Rented - Red
      case 'maintenance': return '#9E9E9E'; // Maintenance - Gray
      default: return '#2196F3'; // Default - Blue
    }
  }

  private getBoothColorFromNumber(status?: number): string {
    switch (status) {
      case 1: return '#4CAF50'; // Available - Green
      case 2: return '#FF9800'; // Reserved - Orange
      case 3: return '#F44336'; // Rented - Red
      case 4: return '#9E9E9E'; // Maintenance - Gray
      default: return '#2196F3'; // Default - Blue
    }
  }

  getBoothStatusSeverity(status: number): string {
    switch (status) {
      case 1: return 'success';
      case 2: return 'warning';
      case 3: return 'danger';
      case 4: return 'secondary';
      default: return 'info';
    }
  }

  getBoothCountByStatus(status: string): number {
    if (this.boothAvailability.length > 0) {
      return this.boothAvailability.filter(b => b.status === status).length;
    }
    // Fallback to booth status if no availability data
    if (!this.currentFloorPlan) return 0;
    const statusMap: { [key: string]: number } = {
      'available': 1,
      'reserved': 2,
      'rented': 3,
      'maintenance': 4
    };
    const statusNum = statusMap[status];
    return this.currentFloorPlan.booths.filter(b => b.booth?.status === statusNum).length;
  }

  applyDateFilter() {
    if (!this.filterStartDate || !this.filterEndDate || !this.currentFloorPlan) {
      return;
    }

    // Validate: end date must be greater than start date
    if (this.filterEndDate <= this.filterStartDate) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Nieprawidłowy zakres dat',
        detail: 'Data "do" musi być późniejsza niż data "od"'
      });
      return;
    }

    this.loading = true;
    const startDateStr = this.formatDate(this.filterStartDate);
    const endDateStr = this.formatDate(this.filterEndDate);

    this.floorPlanService.getBoothsAvailability(
      this.currentFloorPlan.id,
      startDateStr,
      endDateStr
    ).subscribe({
      next: (availability) => {
        this.boothAvailability = availability;
        this.loading = false;

        // Re-render booths with new availability data
        if (this.canvas) {
          this.canvas.clear();
          this.renderElements();
          this.renderBooths();
        }
      },
      error: (error) => {
        console.error('Error loading booth availability:', error);
        this.loading = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Błąd',
          detail: 'Nie udało się pobrać dostępności stanowisk'
        });
      }
    });
  }

  clearDateFilter() {
    this.filterStartDate = undefined;
    this.filterEndDate = undefined;
    this.boothAvailability = [];

    // Re-render booths with default status
    if (this.canvas) {
      this.canvas.clear();
      this.renderElements();
      this.renderBooths();
    }
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  zoomIn() {
    this.currentZoom = Math.min(this.currentZoom * 1.2, this.maxZoom);
    this.applyZoom();
  }

  zoomOut() {
    this.currentZoom = Math.max(this.currentZoom * 0.8, this.minZoom);
    this.applyZoom();
  }

  resetZoom() {
    this.currentZoom = 1;
    this.applyZoom();
    this.centerCanvas();
  }

  private applyZoom() {
    if (this.canvas) {
      this.canvas.setZoom(this.currentZoom);
      this.canvas.renderAll();
    }
  }

  private centerCanvas() {
    if (this.canvas) {
      this.canvas.viewportTransform = [1, 0, 0, 1, 0, 0];
      this.canvas.renderAll();
    }
  }

  /**
   * Get cart items for a specific booth
   */
  private getCartItemsForBooth(boothId: string): CartItemDto[] {
    if (!this.currentCart || !this.currentCart.items) {
      return [];
    }
    return this.currentCart.items.filter(item => item.boothId === boothId);
  }

  /**
   * Check if booth has items in cart
   */
  private hasBoothInCart(boothId: string): boolean {
    return this.getCartItemsForBooth(boothId).length > 0;
  }

  /**
   * Get count of unique booths in cart for current floor plan
   */
  getBoothsInCartCount(): number {
    if (!this.currentCart || !this.currentCart.items || !this.currentFloorPlan) {
      return 0;
    }

    // Get unique booth IDs from current floor plan that are in cart
    const boothIdsInFloorPlan = this.currentFloorPlan.booths.map(b => b.boothId);
    const uniqueBoothsInCart = new Set(
      this.currentCart.items
        .filter(item => boothIdsInFloorPlan.includes(item.boothId))
        .map(item => item.boothId)
    );

    return uniqueBoothsInCart.size;
  }

  trackByFloorPlanId(index: number, floorPlan: FloorPlanDto): string {
    return floorPlan.id;
  }

}