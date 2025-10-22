import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ItemService, ItemSheetService } from '@proxy/items';
import type { ItemDto, ItemSheetDto } from '@proxy/items/models';
import { ItemFormComponent } from '../item-form/item-form.component';
import { ItemBulkCreateComponent } from '../item-bulk-create/item-bulk-create.component';
import { CoreModule } from '@abp/ng.core';

@Component({
  standalone: true,
  selector: 'app-item-list',
  templateUrl: './item-list.component.html',
  styleUrls: ['./item-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    ConfirmDialogModule,
    ToastModule,
    DialogModule,
    DropdownModule,
    ItemFormComponent,
    ItemBulkCreateComponent,
    CoreModule
  ],
  providers: [MessageService, ConfirmationService]
})
export class ItemListComponent implements OnInit {
  items: ItemDto[] = [];
  totalCount = 0;
  loading = false;

  // Pagination
  first = 0;
  rows = 10;

  // Dialog
  displayDialog = false;
  displayBulkCreateDialog = false;
  selectedItem: ItemDto | null = null;

  // Mass selection
  selectedItems: ItemDto[] = [];
  displayAssignDialog = false;
  availableSheets: ItemSheetDto[] = [];
  selectedSheetId: string | null = null;
  loadingSheets = false;
  assigning = false;

  constructor(
    private itemService: ItemService,
    private itemSheetService: ItemSheetService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadItems();
  }

  get selectableItems(): ItemDto[] {
    return this.items.filter(item => item.status === 'Draft');
  }

  isRowSelectable = (event: any): boolean => {
    return event.data.status === 'Draft';
  }

  loadItems(): void {
    this.loading = true;

    this.itemService.getMyItems({
      skipCount: this.first,
      maxResultCount: this.rows
    }).subscribe({
      next: (result) => {
        this.items = result.items || [];
        this.totalCount = result.totalCount || 0;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onPageChange(event: any): void {
    this.first = event.first;
    this.rows = event.rows;
    this.loadItems();
  }

  onHeaderCheckboxChange(event: any): void {
    if (event.checked) {
      // Select only selectable items (Draft status)
      this.selectedItems = [...this.selectableItems];
    } else {
      // Deselect all
      this.selectedItems = [];
    }
    this.cdr.markForCheck();
  }

  onRowSelect(event: any): void {
    // Only allow selection of Draft items
    const item = event.data;
    if (item.status !== 'Draft') {
      // Remove non-Draft items from selection
      this.selectedItems = this.selectedItems.filter(
        (selected) => selected.id !== item.id
      );
      this.cdr.markForCheck();
    }
  }

  onRowUnselect(event: any): void {
    // No action needed, just let it unselect
  }

  openCreateDialog(): void {
    this.selectedItem = null;
    this.displayDialog = true;
  }

  openBulkCreateDialog(): void {
    this.displayBulkCreateDialog = true;
  }

  openEditDialog(item: ItemDto): void {
    this.selectedItem = item;
    this.displayDialog = true;
  }

  onDialogSaved(): void {
    this.loadItems();
  }

  deleteItem(item: ItemDto): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${item.name}"?`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        if (item.id) {
          this.itemService.delete(item.id).subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Item deleted successfully'
              });
              this.loadItems();
            },
            error: (error) => {
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error.error?.error?.message || 'Failed to delete item'
              });
            }
          });
        }
      }
    });
  }

  openAssignToSheetDialog(): void {
    if (this.selectedItems.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select at least one item'
      });
      return;
    }

    this.loadAvailableSheets();
    this.displayAssignDialog = true;
  }

  loadAvailableSheets(): void {
    this.loadingSheets = true;

    // Load Draft and Ready sheets
    this.itemSheetService.getMyItemSheets({
      maxResultCount: 100,
      skipCount: 0
    }).subscribe({
      next: (result) => {
        this.availableSheets = (result.items || []).filter(
          sheet => sheet.status === 'Draft' || sheet.status === 'Ready'
        );
        this.loadingSheets = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load sheets'
        });
        this.loadingSheets = false;
        this.cdr.markForCheck();
      }
    });
  }

  assignToSheet(): void {
    if (!this.selectedSheetId) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please select a sheet'
      });
      return;
    }

    this.assigning = true;

    // Collect all item IDs
    const itemIds = this.selectedItems
      .filter(item => item.id)
      .map(item => item.id!);

    // Use batch endpoint to add all items at once
    this.itemSheetService.batchAddItems({
      sheetId: this.selectedSheetId,
      itemIds: itemIds,
      commissionPercentage: 0 // Will be set from BoothType on backend
    }).subscribe({
      next: (result) => {
        const successCount = result.successCount || 0;
        const errorCount = result.failureCount || 0;
        this.finishAssignment(successCount, errorCount);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to add items to sheet'
        });
        this.assigning = false;
        this.displayAssignDialog = false;
      }
    });
  }

  finishAssignment(successCount: number, errorCount: number): void {
    this.assigning = false;
    this.displayAssignDialog = false;
    this.selectedItems = [];
    this.selectedSheetId = null;

    if (successCount > 0) {
      this.messageService.add({
        severity: 'success',
        summary: 'Success',
        detail: `${successCount} item(s) added to sheet successfully`
      });
    }

    if (errorCount > 0) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: `Failed to add ${errorCount} item(s)`
      });
    }

    this.loadItems();
    this.cdr.markForCheck();
  }
}
