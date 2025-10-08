import {
  Component,
  OnInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  AfterViewInit,
  ChangeDetectorRef
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Canvas, Rect, Text, Group, Shadow, FabricImage } from 'fabric';
import { v4 as uuidv4 } from 'uuid';
import { MessageService } from 'primeng/api';
import { LazyLoadEvent } from 'primeng/api';
import { FloorPlanService } from '../services/floor-plan.service';
import { BoothService } from '../services/booth.service';
import {
  FloorPlanDto,
  CreateFloorPlanDto,
  UpdateFloorPlanDto,
  FloorPlanBoothDto,
  CreateFloorPlanBoothDto,
  BoothPosition,
  ElementPosition
} from '../shared/models/floor-plan.model';
import { BoothDto } from '../shared/models/booth.model';
import { FloorPlanElementType } from '../proxy/domain/floor-plans/floor-plan-element-type.enum';
import { CreateFloorPlanElementDto } from '../proxy/floor-plans/models';

@Component({
  selector: 'app-floor-plan-editor',
  standalone: false,
  template: `
    <div class="floor-plan-editor">
      <!-- Toolbar -->
      <div class="toolbar p-3 border-bottom">
        <div class="flex justify-content-between align-items-center">
          <div class="flex align-items-center gap-3">
            <h3 class="m-0">{{ isEditMode ? 'Edytuj Plan Sali' : 'Nowy Plan Sali' }}</h3>
            <p-badge
              [value]="currentLevel"
              [severity]="currentLevel === 0 ? 'success' : 'info'"
              class="ml-2">
            </p-badge>
          </div>

          <div class="flex align-items-center gap-2">
            <!-- Level Controls -->
            <div class="flex align-items-center gap-2">
              <label>Poziom:</label>
              <p-inputNumber
                [(ngModel)]="currentLevel"
                (onInput)="onLevelChange()"
                [min]="0"
                [max]="50"
                [step]="1"
                [showButtons]="true"
                buttonLayout="vertical"
                spinnerMode="vertical"
                size="small"
                [inputStyle]="{'width': '60px'}"
                styleClass="level-input">
              </p-inputNumber>
            </div>

            <p-divider layout="vertical"></p-divider>

            <!-- Canvas Controls -->
            <p-button
              icon="pi pi-search-plus"
              [text]="true"
              [rounded]="true"
              (onClick)="zoomIn()"
              pTooltip="Powiększ">
            </p-button>
            <p-button
              icon="pi pi-search-minus"
              [text]="true"
              [rounded]="true"
              (onClick)="zoomOut()"
              pTooltip="Pomniejsz">
            </p-button>
            <p-button
              icon="pi pi-refresh"
              [text]="true"
              [rounded]="true"
              (onClick)="resetZoom()"
              pTooltip="Reset widoku">
            </p-button>

            <p-divider layout="vertical"></p-divider>

            <!-- Actions -->
            <p-button
              label="Zapisz"
              icon="pi pi-save"
              (onClick)="save()"
              [loading]="saving">
            </p-button>
            <p-button
              label="Publikuj"
              icon="pi pi-eye"
              severity="success"
              (onClick)="publish()"
              [disabled]="!currentFloorPlan?.id"
              [loading]="publishing">
            </p-button>
          </div>
        </div>

        <!-- Floor Plan Details -->
        <div class="flex align-items-center gap-3 mt-3">
          <div class="flex align-items-center gap-2">
            <label>Nazwa:</label>
            <p-inputText
              [(ngModel)]="floorPlanName"
              placeholder="Wprowadź nazwę planu"
              [style]="{'width': '200px'}">
            </p-inputText>
          </div>

          <div class="flex align-items-center gap-2">
            <label>Wymiary:</label>
            <p-inputNumber
              [(ngModel)]="canvasWidth"
              (input)="onCanvasSizeChange()"
              [min]="400"
              [max]="5000"
              [step]="50"
              [showButtons]="true"
              suffix="px"
              size="small"
              [style]="{'width': '120px'}">
            </p-inputNumber>
            <span>×</span>
            <p-inputNumber
              [(ngModel)]="canvasHeight"
              (input)="onCanvasSizeChange()"
              [min]="300"
              [max]="5000"
              [step]="50"
              [showButtons]="true"
              suffix="px"
              size="small"
              [style]="{'width': '120px'}">
            </p-inputNumber>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <div class="flex" style="height: calc(100vh - 180px);">
        <!-- Panels -->
        <div class="side-panel p-3 border-right" style="width: 320px; overflow-y: auto;">
          <p-tabView>
            <!-- Stanowiska Tab -->
            <p-tabPanel header="Stanowiska">
              <h4 class="mt-0">Dostępne Stanowiska</h4>

          <!-- Booth Filters -->
          <div class="booth-filters mb-3">
            <div class="field mb-2">
              <label class="text-sm">Szukaj:</label>
              <p-inputText
                [(ngModel)]="boothFilterText"
                placeholder="Numer stanowiska..."
                (input)="onBoothFilterChange()"
                class="w-full"
                size="small">
              </p-inputText>
            </div>
            <div class="field mb-2">
              <label class="text-sm">Status:</label>
              <p-dropdown
                [(ngModel)]="boothFilterStatus"
                [options]="statusOptions"
                placeholder="Wszystkie statusy"
                [showClear]="true"
                optionLabel="label"
                optionValue="value"
                (onChange)="onBoothFilterChange()"
                class="w-full"
                size="small">
              </p-dropdown>
            </div>
          </div>

          <div class="available-booths">
            <p-scroller
              [items]="filteredBooths"
              [itemSize]="60"
              scrollHeight="400px"
              [lazy]="true"
              (onLazyLoad)="loadBoothsLazy($event)">

              <ng-template pTemplate="item" let-booth>
                <div class="booth-item p-2 mb-2 border-round cursor-pointer border"
                     [class.selected]="selectedBoothId === booth.id"
                     (click)="selectBooth(booth)"
                     draggable="true"
                     (dragstart)="onBoothDragStart($event, booth)">
                  <div class="flex justify-content-between align-items-center">
                    <div>
                      <strong>{{ booth.number }}</strong>
                      <div class="text-sm text-color-secondary">
                        {{ booth.pricePerDay }} {{ booth.currencyDisplayName }}/dzień
                      </div>
                    </div>
                    <p-badge
                      [value]="booth.statusDisplayName"
                      [severity]="getBoothStatusSeverity(booth.status)">
                    </p-badge>
                  </div>
                </div>
              </ng-template>
            </p-scroller>

            <div *ngIf="filteredBooths.length === 0" class="text-center p-3 text-color-secondary">
              <i class="pi pi-info-circle"></i>
              <div class="mt-2">Brak stanowisk spełniających kryteria</div>
            </div>
          </div>

          <!-- Booth Properties Panel -->
          <div *ngIf="selectedBoothOnCanvas" class="mt-4">
            <h4>Właściwości Stanowiska</h4>
            <div class="properties-panel">
              <div class="field">
                <label>Pozycja X:</label>
                <p-inputNumber
                  [(ngModel)]="selectedBoothOnCanvas.x"
                  (input)="updateSelectedBoothPosition()"
                  [min]="0"
                  [step]="1">
                </p-inputNumber>
              </div>
              <div class="field">
                <label>Pozycja Y:</label>
                <p-inputNumber
                  [(ngModel)]="selectedBoothOnCanvas.y"
                  (input)="updateSelectedBoothPosition()"
                  [min]="0"
                  [step]="1">
                </p-inputNumber>
              </div>
              <div class="field">
                <label>Szerokość:</label>
                <p-inputNumber
                  [(ngModel)]="selectedBoothOnCanvas.width"
                  (input)="updateSelectedBoothPosition()"
                  [min]="20"
                  [max]="500"
                  [step]="5">
                </p-inputNumber>
              </div>
              <div class="field">
                <label>Wysokość:</label>
                <p-inputNumber
                  [(ngModel)]="selectedBoothOnCanvas.height"
                  (input)="updateSelectedBoothPosition()"
                  [min]="20"
                  [max]="500"
                  [step]="5">
                </p-inputNumber>
              </div>
              <div class="field">
                <label>Obrót:</label>
                <p-inputNumber
                  [(ngModel)]="selectedBoothOnCanvas.rotation"
                  (input)="updateSelectedBoothPosition()"
                  [min]="0"
                  [max]="359"
                  [step]="15"
                  suffix="°">
                </p-inputNumber>
              </div>
              <p-button
                label="Usuń ze planu"
                icon="pi pi-trash"
                severity="danger"
                size="small"
                [text]="true"
                (onClick)="removeBoothFromCanvas()">
              </p-button>
            </div>
          </div>
            </p-tabPanel>

            <!-- Elementy Tab -->
            <p-tabPanel header="Elementy">
              <h4 class="mt-0">Dostępne Elementy</h4>

              <div class="available-elements">
                <div
                  *ngFor="let elementType of elementTypes; trackBy: trackByElementType"
                  class="element-item p-2 mb-2 border-round cursor-pointer border"
                  draggable="true"
                  (dragstart)="onElementDragStart($event, elementType)">
                  <div class="flex align-items-center gap-2">
                    <i [class]="'pi ' + elementType.icon" [style.color]="elementType.color"></i>
                    <span>{{ elementType.label }}</span>
                  </div>
                </div>
              </div>

              <!-- Element Properties Panel -->
              <div *ngIf="selectedElement" class="mt-4">
                <h4>Właściwości Elementu</h4>
                <div class="properties-panel">
                  <div class="field">
                    <label>Typ:</label>
                    <p-inputText
                      [value]="getElementTypeName()"
                      [disabled]="true"
                      size="small">
                    </p-inputText>
                  </div>
                  <div class="field">
                    <label>Pozycja X:</label>
                    <p-inputNumber
                      [(ngModel)]="selectedElement.x"
                      (input)="updateSelectedElementPosition()"
                      [min]="0"
                      [step]="1"
                      size="small">
                    </p-inputNumber>
                  </div>
                  <div class="field">
                    <label>Pozycja Y:</label>
                    <p-inputNumber
                      [(ngModel)]="selectedElement.y"
                      (input)="updateSelectedElementPosition()"
                      [min]="0"
                      [step]="1"
                      size="small">
                    </p-inputNumber>
                  </div>
                  <div class="field">
                    <label>Szerokość:</label>
                    <p-inputNumber
                      [(ngModel)]="selectedElement.width"
                      (input)="updateSelectedElementPosition()"
                      [min]="10"
                      [max]="500"
                      [step]="5"
                      size="small">
                    </p-inputNumber>
                  </div>
                  <div class="field">
                    <label>Wysokość:</label>
                    <p-inputNumber
                      [(ngModel)]="selectedElement.height"
                      (input)="updateSelectedElementPosition()"
                      [min]="10"
                      [max]="500"
                      [step]="5"
                      size="small">
                    </p-inputNumber>
                  </div>
                  <div class="field">
                    <label>Obrót:</label>
                    <p-inputNumber
                      [(ngModel)]="selectedElement.rotation"
                      (input)="updateSelectedElementPosition()"
                      [min]="0"
                      [max]="359"
                      [step]="15"
                      suffix="°"
                      size="small">
                    </p-inputNumber>
                  </div>
                  <div class="field" *ngIf="selectedElement && selectedElement.elementType === 11">
                    <label>Tekst:</label>
                    <input
                      type="text"
                      [(ngModel)]="selectedElement.text"
                      (input)="updateElementText()"
                      placeholder="Wprowadź tekst..."
                      class="p-inputtext p-component p-inputtext-sm">
                  </div>
                  <p-button
                    label="Usuń element"
                    icon="pi pi-trash"
                    severity="danger"
                    size="small"
                    [text]="true"
                    (onClick)="removeElementFromCanvas()">
                  </p-button>
                </div>
              </div>
            </p-tabPanel>
          </p-tabView>
        </div>

        <!-- Canvas Area -->
        <div class="canvas-container flex-1 relative"
             [class.drag-over]="isDragOver"
             (drop)="onCanvasDrop($event)"
             (dragover)="onCanvasDragOver($event)"
             (dragleave)="onCanvasDragLeave($event)">
          <canvas
            #fabricCanvas
            [width]="canvasWidth"
            [height]="canvasHeight">
          </canvas>

          <!-- Canvas Overlay Info -->
          <div class="canvas-info absolute top-0 right-0 p-2 bg-white border-round shadow-2 m-2">
            <div class="text-sm">
              <div>Zoom: {{ (currentZoom * 100).toFixed(0) }}%</div>
              <div>Stanowiska: {{ boothsOnCanvas.length }}</div>
              <div>Elementy: {{ elementsOnCanvas.length }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .floor-plan-editor {
      height: 100vh;
      display: flex;
      flex-direction: column;
    }

    .booth-item, .element-item {
      transition: all 0.2s;
    }

    .booth-item:hover, .element-item:hover {
      background-color: var(--primary-color-text);
      transform: translateY(-1px);
    }

    .booth-item.selected {
      background-color: var(--primary-100);
      border-color: var(--primary-500);
    }

    .element-item:hover {
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .canvas-container {
      background: #f8f9fa;
      overflow: auto;
      min-height: 400px;
    }

    .canvas-container.drag-over {
      background: #e3f2fd;
      border: 2px dashed #2196f3;
    }

    canvas {
      border: 1px solid #dee2e6;
      background: white;
      display: block;
    }

    .properties-panel .field {
      margin-bottom: 1rem;
    }

    .properties-panel label {
      display: block;
      margin-bottom: 0.25rem;
      font-weight: 500;
    }

    .toolbar {
      background: white;
      box-shadow: 0 2px 4px rgba(0,0,0,0.1);
    }

    .booths-panel {
      background: white;
      border-right: 1px solid #dee2e6;
    }

    .booth-filters {
      border-bottom: 1px solid #e9ecef;
      padding-bottom: 1rem;
    }

    .booth-filters .field {
      margin-bottom: 0.5rem;
    }

    .booth-filters label {
      display: block;
      margin-bottom: 0.25rem;
      font-weight: 500;
      color: var(--text-color-secondary);
    }
  `]
})
export class FloorPlanEditorComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('fabricCanvas', { static: true }) canvasElement!: ElementRef<HTMLCanvasElement>;

  canvas!: Canvas;
  currentFloorPlan?: FloorPlanDto;
  availableBooths: BoothDto[] = [];
  filteredBooths: BoothDto[] = [];
  boothsOnCanvas: BoothPosition[] = [];
  selectedBoothId?: string;
  selectedBoothOnCanvas?: BoothPosition;

  // Elements
  elementsOnCanvas: ElementPosition[] = [];
  selectedElement?: ElementPosition;
  elementTypes = [
    { type: FloorPlanElementType.Wall, label: 'Ściana', icon: 'pi-minus', color: '#9e9e9e', width: 100, height: 10, symbol: '' },
    { type: FloorPlanElementType.Door, label: 'Drzwi', icon: 'pi-sign-in', color: '#8d6e63', width: 60, height: 10, symbol: '⇆' },
    { type: FloorPlanElementType.Window, label: 'Okno', icon: 'pi-th-large', color: '#81d4fa', width: 80, height: 10, symbol: '▭' },
    { type: FloorPlanElementType.Pillar, label: 'Kolumna', icon: 'pi-stop', color: '#757575', width: 40, height: 40, symbol: '●' },
    { type: FloorPlanElementType.Checkout, label: 'Kasa', icon: 'pi-dollar', color: '#4caf50', width: 60, height: 50, symbol: '$' },
    { type: FloorPlanElementType.Restroom, label: 'Toaleta', icon: 'pi-users', color: '#2196f3', width: 60, height: 60, symbol: '🚻' },
    { type: FloorPlanElementType.InfoDesk, label: 'Informacja', icon: 'pi-info-circle', color: '#ff9800', width: 80, height: 60, symbol: 'ℹ' },
    { type: FloorPlanElementType.EmergencyExit, label: 'Wyjście', icon: 'pi-sign-out', color: '#f44336', width: 70, height: 60, symbol: '🚪' },
    { type: FloorPlanElementType.Storage, label: 'Magazyn', icon: 'pi-box', color: '#9c27b0', width: 80, height: 70, symbol: '📦' },
    { type: FloorPlanElementType.TextLabel, label: 'Etykieta', icon: 'pi-tag', color: '#607d8b', width: 100, height: 30, symbol: '' },
    { type: FloorPlanElementType.Zone, label: 'Strefa', icon: 'pi-circle', color: '#4caf5033', width: 150, height: 150, symbol: '' }
  ];

  // Booth filtering
  boothFilterText = '';
  boothFilterStatus?: number;

  // Floor plan properties
  floorPlanName = '';
  currentLevel = 0;
  canvasWidth = 1200;
  canvasHeight = 800;
  currentZoom = 1;

  // State
  isEditMode = false;
  saving = false;
  publishing = false;
  isDragOver = false;

  // Status options for filtering
  statusOptions = [
    { label: 'Dostępne', value: 1 },
    { label: 'Zarezerwowane', value: 2 },
    { label: 'Wynajęte', value: 3 },
    { label: 'Konserwacja', value: 4 }
  ];

  constructor(
    private floorPlanService: FloorPlanService,
    private boothService: BoothService,
    private messageService: MessageService,
    private cdr: ChangeDetectorRef,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadAvailableBooths();

    // Check if we're editing an existing floor plan
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode = true;
      this.loadFloorPlan(id);
    } else {
      this.isEditMode = false;
      this.initializeNewFloorPlan();
    }
  }

  ngAfterViewInit() {
    this.initializeFabricCanvas();
  }

  ngOnDestroy() {
    if (this.canvas) {
      this.canvas.dispose();
    }
  }

  private initializeFabricCanvas() {
    this.canvas = new Canvas(this.canvasElement.nativeElement, {
      width: this.canvasWidth,
      height: this.canvasHeight,
      backgroundColor: '#ffffff',
      selection: true
    });

    // Canvas event listeners
    this.canvas.on('selection:created', (e) => this.onCanvasSelectionChanged(e));
    this.canvas.on('selection:updated', (e) => this.onCanvasSelectionChanged(e));
    this.canvas.on('selection:cleared', () => this.onCanvasSelectionCleared());
    this.canvas.on('object:modified', (e) => this.onObjectModified(e));
  }

  private loadAvailableBooths() {
    const listInput = {
      skipCount: 0,
      maxResultCount: 50, // Load first page only
      filter: this.boothFilterText,
      status: this.boothFilterStatus
    };

    this.boothService.getList(listInput).subscribe({
      next: (result) => {
        this.availableBooths = result.items;
        this.applyBoothFilters();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Błąd',
          detail: 'Nie udało się pobrać stanowisk'
        });
      }
    });
  }

  private applyBoothFilters() {
    this.filteredBooths = this.availableBooths.filter(booth => {
      const matchesText = !this.boothFilterText ||
        booth.number.toLowerCase().includes(this.boothFilterText.toLowerCase());

      const matchesStatus = this.boothFilterStatus === undefined || booth.status === this.boothFilterStatus;

      return matchesText && matchesStatus;
    });
  }

  onBoothFilterChange() {
    this.applyBoothFilters();
  }

  loadBoothsLazy(event: LazyLoadEvent) {
    const listInput = {
      skipCount: event.first || 0,
      maxResultCount: event.rows || 50,
      filter: this.boothFilterText,
      status: this.boothFilterStatus
    };

    this.boothService.getList(listInput).subscribe({
      next: (result) => {
        this.filteredBooths = [...this.filteredBooths, ...result.items];
      }
    });
  }

  private initializeNewFloorPlan() {
    this.floorPlanName = this.currentLevel === 0
      ? 'Plan Parteru'
      : `Plan Piętra ${this.currentLevel}`;
    this.boothsOnCanvas = [];
    this.elementsOnCanvas = [];
    this.selectedBoothOnCanvas = undefined;
    this.selectedElement = undefined;
    this.currentFloorPlan = undefined;
  }

  private loadFloorPlan(id: string) {
    this.floorPlanService.get(id).subscribe({
      next: (floorPlan) => {
        this.currentFloorPlan = floorPlan;
        this.floorPlanName = floorPlan.name;
        this.currentLevel = floorPlan.level;
        this.canvasWidth = floorPlan.width;
        this.canvasHeight = floorPlan.height;

        // Update canvas dimensions if already initialized
        if (this.canvas) {
          this.canvas.setDimensions({
            width: this.canvasWidth,
            height: this.canvasHeight
          });
        }

        // Load booths positions
        if (floorPlan.booths && floorPlan.booths.length > 0) {
          this.boothsOnCanvas = floorPlan.booths.map(fpb => ({
            boothId: fpb.boothId,
            x: fpb.x,
            y: fpb.y,
            width: fpb.width,
            height: fpb.height,
            rotation: fpb.rotation,
            booth: fpb.booth
          }));
        }

        // Load elements positions
        if (floorPlan.elements && floorPlan.elements.length > 0) {
          this.elementsOnCanvas = floorPlan.elements.map(el => ({
            id: el.id,
            elementType: el.elementType!,
            x: el.x,
            y: el.y,
            width: el.width,
            height: el.height,
            rotation: el.rotation,
            color: el.color,
            text: el.text,
            iconName: el.iconName,
            thickness: el.thickness,
            opacity: el.opacity,
            direction: el.direction
          }));
        }

        // Render booths and elements on canvas when it's ready
        setTimeout(() => {
          this.renderExistingBooths();
          this.renderExistingElements();
        }, 100);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Błąd',
          detail: 'Nie udało się załadować planu sali'
        });
        // Navigate back to list on error
        this.router.navigate(['/floor-plans/list']);
      }
    });
  }

  private renderExistingBooths() {
    if (!this.canvas || this.boothsOnCanvas.length === 0) return;

    this.boothsOnCanvas.forEach(boothPosition => {
      this.renderBoothOnCanvas(boothPosition);
    });
  }

  private renderExistingElements() {
    if (!this.canvas || this.elementsOnCanvas.length === 0) return;

    this.elementsOnCanvas.forEach(element => {
      this.renderElementOnCanvas(element);
    });
  }

  selectBooth(booth: BoothDto) {
    this.selectedBoothId = booth.id;
  }

  onBoothDragStart(event: DragEvent, booth: BoothDto) {
    if (event.dataTransfer) {
      event.dataTransfer.setData('text/plain', JSON.stringify({
        type: 'booth',
        booth: booth
      }));
    }
  }

  onCanvasDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = true;
  }

  onCanvasDragLeave(event: DragEvent) {
    this.isDragOver = false;
  }

  onCanvasDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragOver = false;

    const data = event.dataTransfer?.getData('text/plain');
    if (!data) return;

    try {
      const dragData = JSON.parse(data);
      const rect = this.canvasElement.nativeElement.getBoundingClientRect();
      const x = event.clientX - rect.left;
      const y = event.clientY - rect.top;

      if (x >= 0 && y >= 0 && x <= this.canvasWidth && y <= this.canvasHeight) {
        if (dragData.type === 'booth') {
          this.addBoothToCanvas(dragData.booth, x, y);
        } else if (dragData.type === 'element') {
          this.addElementToCanvas(dragData.elementType, x, y);
        }
      }
    } catch (error) {
      console.error('Error parsing drag data:', error);
    }
  }

  private addBoothToCanvas(booth: BoothDto, x: number, y: number) {
    // Check if booth is already on canvas
    if (this.boothsOnCanvas.find(b => b.boothId === booth.id)) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Uwaga',
        detail: 'Stanowisko jest już na planie'
      });
      return;
    }

    const boothPosition: BoothPosition = {
      boothId: booth.id,
      x: Math.max(0, x - 25),
      y: Math.max(0, y - 25),
      width: 80,
      height: 60,
      rotation: 0,
      booth: booth
    };

    this.boothsOnCanvas.push(boothPosition);
    this.renderBoothOnCanvas(boothPosition);
  }

  private renderBoothOnCanvas(boothPosition: BoothPosition) {
    const rect = new Rect({
      left: boothPosition.x,
      top: boothPosition.y,
      width: boothPosition.width,
      height: boothPosition.height,
      fill: this.getBoothColor(boothPosition.booth?.status),
      stroke: '#333',
      strokeWidth: 2,
      cornerSize: 8,
      transparentCorners: false,
      cornerColor: '#4CAF50',
      cornerStyle: 'circle',
      angle: boothPosition.rotation
    });

    const text = new Text(boothPosition.booth?.number || '', {
      left: boothPosition.x + boothPosition.width / 2,
      top: boothPosition.y + boothPosition.height / 2,
      fontSize: 14,
      fontFamily: 'Arial',
      fill: '#333',
      textAlign: 'center',
      originX: 'center',
      originY: 'center',
      selectable: false,
      evented: false
    });

    const group = new Group([rect, text], {
      left: boothPosition.x,
      top: boothPosition.y,
      selectable: true,
      hasControls: true,
      hasBorders: true
    });

    (group as any).boothId = boothPosition.boothId;
    this.canvas.add(group);
    this.canvas.renderAll();
  }

  private getBoothColor(status?: number): string {
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

  private onCanvasSelectionChanged(e: any) {
    const activeObject = e.target || e.selected?.[0];
    if (activeObject && (activeObject as any).boothId) {
      const boothId = (activeObject as any).boothId;
      this.selectedBoothOnCanvas = this.boothsOnCanvas.find(b => b.boothId === boothId);
      this.selectedElement = undefined;
      this.cdr.detectChanges();
    } else if (activeObject && (activeObject as any).elementId) {
      const elementId = (activeObject as any).elementId;
      this.selectedElement = this.elementsOnCanvas.find(e => e.id === elementId);
      this.selectedBoothOnCanvas = undefined;
      this.cdr.detectChanges();
    }
  }

  private onCanvasSelectionCleared() {
    this.selectedBoothOnCanvas = undefined;
    this.selectedElement = undefined;
    this.cdr.detectChanges();
  }

  private onObjectModified(e: any) {
    const obj = e.target;
    if (obj && (obj as any).boothId) {
      const boothId = (obj as any).boothId;
      const boothPosition = this.boothsOnCanvas.find(b => b.boothId === boothId);
      if (boothPosition) {
        boothPosition.x = obj.left;
        boothPosition.y = obj.top;
        boothPosition.width = obj.width * obj.scaleX;
        boothPosition.height = obj.height * obj.scaleY;
        boothPosition.rotation = obj.angle;
        this.cdr.detectChanges();
      }
    } else if (obj && (obj as any).elementId) {
      const elementId = (obj as any).elementId;
      const element = this.elementsOnCanvas.find(e => e.id === elementId);
      if (element) {
        element.x = obj.left;
        element.y = obj.top;
        element.width = obj.width * (obj.scaleX || 1);
        element.height = obj.height * (obj.scaleY || 1);
        element.rotation = obj.angle;
        this.cdr.detectChanges();
      }
    }
  }

  updateSelectedBoothPosition() {
    if (!this.selectedBoothOnCanvas) return;

    const activeObject = this.canvas.getActiveObject();
    if (activeObject && (activeObject as any).boothId === this.selectedBoothOnCanvas.boothId) {
      activeObject.set({
        left: this.selectedBoothOnCanvas.x,
        top: this.selectedBoothOnCanvas.y,
        width: this.selectedBoothOnCanvas.width,
        height: this.selectedBoothOnCanvas.height,
        angle: this.selectedBoothOnCanvas.rotation
      });

      this.canvas.renderAll();
    }
  }

  removeBoothFromCanvas() {
    if (!this.selectedBoothOnCanvas) return;

    const activeObject = this.canvas.getActiveObject();
    if (activeObject) {
      this.canvas.remove(activeObject);
    }

    this.boothsOnCanvas = this.boothsOnCanvas.filter(
      b => b.boothId !== this.selectedBoothOnCanvas!.boothId
    );

    this.selectedBoothOnCanvas = undefined;
    this.canvas.discardActiveObject();
    this.canvas.renderAll();
  }

  onLevelChange() {
    // Auto-update name based on level, but only if it's the default name pattern
    const isDefaultName = this.floorPlanName.startsWith('Plan Piętra') ||
                          this.floorPlanName.startsWith('Plan Parteru') ||
                          !this.floorPlanName;

    if (isDefaultName) {
      this.floorPlanName = this.currentLevel === 0
        ? 'Plan Parteru'
        : `Plan Piętra ${this.currentLevel}`;
    }
  }

  onCanvasSizeChange() {
    if (this.canvas) {
      this.canvas.setDimensions({
        width: this.canvasWidth,
        height: this.canvasHeight
      });
      this.canvas.renderAll();
    }
  }

  zoomIn() {
    this.currentZoom = Math.min(this.currentZoom * 1.1, 3);
    this.canvas.setZoom(this.currentZoom);
  }

  zoomOut() {
    this.currentZoom = Math.max(this.currentZoom * 0.9, 0.1);
    this.canvas.setZoom(this.currentZoom);
  }

  resetZoom() {
    this.currentZoom = 1;
    this.canvas.setZoom(1);
    this.canvas.viewportTransform = [1, 0, 0, 1, 0, 0];
    this.canvas.renderAll();
  }

  // Element methods
  onElementDragStart(event: DragEvent, elementType: any) {
    if (event.dataTransfer) {
      event.dataTransfer.setData('text/plain', JSON.stringify({ type: 'element', elementType }));
    }
  }

  addElementToCanvas(elementType: any, x: number, y: number) {
    const element: ElementPosition = {
      id: uuidv4(),
      elementType: elementType.type,
      x: Math.max(0, x),
      y: Math.max(0, y),
      width: elementType.width,
      height: elementType.height,
      rotation: 0,
      color: elementType.color,
      opacity: elementType.type === FloorPlanElementType.Zone ? 0.3 : 1
    };
    this.elementsOnCanvas.push(element);
    this.renderElementOnCanvas(element);
  }

  private async renderElementOnCanvas(element: ElementPosition) {
    const svgString = this.getElementSvg(element.elementType, element.width, element.height, element.color);

    if (svgString && element.elementType !== FloorPlanElementType.TextLabel) {
      // Load SVG graphic
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
          selectable: true,
          hasControls: true,
          hasBorders: true
        });
        (img as any).elementId = element.id;
        this.canvas.add(img);
        this.canvas.renderAll();
      } catch (error) {
        console.error('Error loading SVG:', error);
        // Fallback to simple rect if SVG fails
        this.renderSimpleElement(element);
      }
    } else {
      // Fallback for TextLabel and others
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
      opacity: element.opacity || 1
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
        selectable: true,
        hasControls: true,
        hasBorders: true
      });

      (group as any).elementId = element.id;
      this.canvas.add(group);
    } else {
      (rect as any).elementId = element.id;
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

  updateSelectedElementPosition() {
    if (!this.selectedElement) return;
    const activeObject = this.canvas.getActiveObject();
    if (activeObject && (activeObject as any).elementId === this.selectedElement.id) {
      activeObject.set({ left: this.selectedElement.x, top: this.selectedElement.y, width: this.selectedElement.width, height: this.selectedElement.height, angle: this.selectedElement.rotation });
      this.canvas.renderAll();
    }
  }

  removeElementFromCanvas() {
    if (!this.selectedElement) return;
    const activeObject = this.canvas.getActiveObject();
    if (activeObject) this.canvas.remove(activeObject);
    this.elementsOnCanvas = this.elementsOnCanvas.filter(e => e.id !== this.selectedElement!.id);
    this.selectedElement = undefined;
    this.canvas.discardActiveObject();
    this.canvas.renderAll();
  }

  getElementTypeName(): string {
    if (!this.selectedElement) return '';
    return this.elementTypes.find(t => t.type === this.selectedElement!.elementType)?.label || '';
  }

  updateElementText() {
    if (!this.selectedElement || this.selectedElement.elementType !== FloorPlanElementType.TextLabel) return;

    // Find and remove the old element from canvas
    const activeObject = this.canvas.getActiveObject();
    if (activeObject) {
      this.canvas.remove(activeObject);
    }

    // Re-render the element with updated text
    this.renderElementOnCanvas(this.selectedElement);

    // Re-select the newly rendered element
    const newObject = this.canvas.getObjects().find(obj => (obj as any).elementId === this.selectedElement!.id);
    if (newObject) {
      this.canvas.setActiveObject(newObject);
    }

    this.canvas.renderAll();
  }

  save() {
    if (!this.floorPlanName.trim()) {
      this.messageService.add({
        severity: 'error',
        summary: 'Błąd',
        detail: 'Nazwa planu jest wymagana'
      });
      return;
    }

    this.saving = true;

    const floorPlanData: CreateFloorPlanDto = {
      name: this.floorPlanName,
      level: Math.round(this.currentLevel),
      width: Math.round(this.canvasWidth),
      height: Math.round(this.canvasHeight),
      booths: this.boothsOnCanvas.map(booth => ({
        boothId: booth.boothId,
        x: Math.round(booth.x),
        y: Math.round(booth.y),
        width: Math.round(booth.width),
        height: Math.round(booth.height),
        rotation: Math.round(booth.rotation)
      })),
      elements: this.elementsOnCanvas.map(el => ({
        elementType: el.elementType,
        x: Math.round(el.x),
        y: Math.round(el.y),
        width: Math.round(el.width),
        height: Math.round(el.height),
        rotation: Math.round(el.rotation),
        color: el.color,
        text: el.text,
        iconName: el.iconName,
        thickness: el.thickness,
        opacity: el.opacity,
        direction: el.direction
      } as CreateFloorPlanElementDto))
    };

    const request = this.isEditMode && this.currentFloorPlan?.id
      ? this.floorPlanService.update(this.currentFloorPlan.id, floorPlanData)
      : this.floorPlanService.create(floorPlanData);

    request.subscribe({
      next: (floorPlan) => {
        const wasNewPlan = !this.isEditMode;
        this.currentFloorPlan = floorPlan;
        this.isEditMode = true;
        this.saving = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Sukces',
          detail: 'Plan sali został zapisany'
        });

        // If it was a new plan, navigate to edit mode with the new ID
        if (wasNewPlan) {
          this.router.navigate(['/floor-plans/editor', floorPlan.id], { replaceUrl: true });
        }
      },
      error: (error) => {
        this.saving = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Błąd',
          detail: 'Nie udało się zapisać planu sali'
        });
      }
    });
  }

  publish() {
    if (!this.currentFloorPlan) {
      this.messageService.add({
        severity: 'error',
        summary: 'Błąd',
        detail: 'Najpierw zapisz plan sali'
      });
      return;
    }

    this.publishing = true;

    this.floorPlanService.publish(this.currentFloorPlan.id).subscribe({
      next: (floorPlan) => {
        this.currentFloorPlan = floorPlan;
        this.publishing = false;

        this.messageService.add({
          severity: 'success',
          summary: 'Sukces',
          detail: 'Plan sali został opublikowany'
        });
      },
      error: (error) => {
        this.publishing = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Błąd',
          detail: 'Nie udało się opublikować planu sali'
        });
      }
    });
  }

  trackByBoothId(index: number, booth: BoothDto): string {
    return booth.id;
  }

  trackByElementType(index: number, elementType: any): number {
    return elementType.type;
  }
}