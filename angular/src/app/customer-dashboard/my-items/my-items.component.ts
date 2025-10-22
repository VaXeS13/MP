import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MyItemService } from '@proxy/application/customer-dashboard';
import { MyItemDto, GetMyItemsDto } from '@proxy/application/contracts/customer-dashboard';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-my-items',
  templateUrl: './my-items.component.html',
  styleUrls: ['./my-items.component.scss'],
  standalone: false
})
export class MyItemsComponent implements OnInit, OnDestroy {
  items: MyItemDto[] = [];
  loading = false;
  displayDialog = false;
  selectedItem: MyItemDto | null = null;
  isEditMode = false;
  totalItems = 0;
  currentPage = 0;
  pageSize = 10;

  filterStatus: string | null = null;
  filterCategory: string | null = null;
  categories: string[] = [];

  private destroy$ = new Subject<void>();

  statuses = [
    { label: 'Na sprzedaż', value: 'ForSale' },
    { label: 'Sprzedane', value: 'Sold' },
    { label: 'Odebrane', value: 'Reclaimed' }
  ];

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private myItemService: MyItemService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.loadCategories();

    // Subscribe to query params for filtering
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        this.filterStatus = params['filterStatus'] || null;
        this.filterCategory = params['filterCategory'] || null;
        this.currentPage = 0;
        this.loadItems();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadCategories(): void {
    this.myItemService.getMyItemCategories()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (categories) => {
          this.categories = categories;
        },
        error: (error) => {
          console.error('Error loading categories:', error);
        }
      });
  }

  loadItems(): void {
    this.loading = true;

    const input: GetMyItemsDto = {
      status: this.filterStatus,
      category: this.filterCategory,
      searchTerm: null,
      rentalId: null,
      createdAfter: null,
      createdBefore: null,
      sorting: 'CreationTime DESC',
      skipCount: this.currentPage * this.pageSize,
      maxResultCount: this.pageSize
    };

    this.myItemService.getMyItems(input)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.items = response.items;
          this.totalItems = response.totalCount;
          this.loading = false;
        },
        error: (error) => {
          console.error('Error loading items:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Błąd',
            detail: 'Nie udało się załadować przedmiotów'
          });
          this.loading = false;
        }
      });
  }

  onPageChange(event: any): void {
    this.currentPage = event.first / event.rows;
    this.pageSize = event.rows;
    this.loadItems();
  }

  navigateToAddItem(): void {
    this.router.navigate(['/items/list']);
  }

  editItem(item: MyItemDto): void {
    // Check if item has QR code generated (barcode exists)
    if (item.barcode) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Edycja zablokowana',
        detail: 'Ten przedmiot ma już wygenerowany kod QR i nie może być edytowany'
      });
      return;
    }

    this.selectedItem = { ...item };
    this.isEditMode = true;
    this.displayDialog = true;
  }

  deleteItem(item: MyItemDto): void {
    if (confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name}"?`)) {
      this.loading = true;

      this.myItemService.delete(item.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Sukces',
              detail: 'Przedmiot został usunięty'
            });
            this.loadItems();
          },
          error: (error) => {
            console.error('Error deleting item:', error);
            this.messageService.add({
              severity: 'error',
              summary: 'Błąd',
              detail: 'Nie udało się usunąć przedmiotu'
            });
            this.loading = false;
          }
        });
    }
  }

  getStatusSeverity(status: string): string {
    switch (status) {
      case 'ForSale': return 'info';
      case 'Sold': return 'success';
      case 'Reclaimed': return 'warning';
      default: return 'secondary';
    }
  }

  getStatusLabel(status: string): string {
    const found = this.statuses.find(s => s.value === status);
    return found ? found.label : status;
  }

  formatCurrency(amount: number | null | undefined): string {
    return new Intl.NumberFormat('pl-PL', {
      style: 'currency',
      currency: 'PLN'
    }).format(amount || 0);
  }

  canEditItem(item: MyItemDto): boolean {
    // Block editing if item has QR code (barcode exists)
    return !item.barcode;
  }

  trackByItemId(index: number, item: MyItemDto): string {
    return item.id;
  }
}
