import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { ItemService } from '@proxy/items';
import type { BulkItemEntryDto, CreateBulkItemsDto } from '@proxy/items/models';
import { TenantCurrencyService } from '../../services/tenant-currency.service';
import { CoreModule } from '@abp/ng.core';

interface BulkItemRow extends BulkItemEntryDto {
  rowId: number;
  hasError?: boolean;
  errorMessage?: string;
}

@Component({
  standalone: true,
  selector: 'app-item-bulk-create',
  templateUrl: './item-bulk-create.component.html',
  styleUrls: ['./item-bulk-create.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    TableModule,
    InputTextModule,
    InputNumberModule,
    CoreModule
  ]
})
export class ItemBulkCreateComponent implements OnInit {
  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() saved = new EventEmitter<void>();

  items: BulkItemRow[] = [];
  saving = false;
  currency: string = 'PLN';
  nextRowId = 1;

  constructor(
    private itemService: ItemService,
    private messageService: MessageService,
    private tenantCurrencyService: TenantCurrencyService
  ) {}

  ngOnInit(): void {
    this.loadTenantCurrency();
    this.addEmptyRow();
  }

  loadTenantCurrency(): void {
    this.tenantCurrencyService.getCurrency().subscribe({
      next: (result) => {
        this.currency = this.tenantCurrencyService.getCurrencyName(result.currency);
      },
      error: () => {
        this.currency = 'PLN'; // Default fallback
      }
    });
  }

  addEmptyRow(): void {
    this.items.push({
      rowId: this.nextRowId++,
      name: '',
      category: undefined,
      price: 0
    });
  }

  removeRow(row: BulkItemRow): void {
    const index = this.items.findIndex(i => i.rowId === row.rowId);
    if (index !== -1) {
      this.items.splice(index, 1);
    }

    // Always keep at least one row
    if (this.items.length === 0) {
      this.addEmptyRow();
    }
  }

  onHide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.resetForm();
  }

  resetForm(): void {
    this.items = [];
    this.nextRowId = 1;
    this.addEmptyRow();
  }

  isValid(): boolean {
    return this.items.some(item =>
      item.name &&
      item.name.trim().length > 0 &&
      item.price > 0
    );
  }

  save(): void {
    // Filter out empty rows
    const validItems = this.items.filter(item =>
      item.name &&
      item.name.trim().length > 0 &&
      item.price > 0
    );

    if (validItems.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'Please add at least one valid item'
      });
      return;
    }

    this.saving = true;

    // Clear previous errors
    this.items.forEach(item => {
      item.hasError = false;
      item.errorMessage = undefined;
    });

    const dto: CreateBulkItemsDto = {
      items: validItems.map(item => ({
        name: item.name,
        category: item.category,
        price: item.price
      }))
    };

    this.itemService.createBulk(dto).subscribe({
      next: (result) => {
        this.saving = false;

        // Show errors for failed items
        if (result.errors && result.errors.length > 0) {
          result.errors.forEach(error => {
            const row = validItems[error.itemIndex];
            if (row) {
              row.hasError = true;
              row.errorMessage = error.errorMessage;
            }
          });
        }

        // Show success message
        if (result.successCount > 0) {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `${result.successCount} item(s) created successfully`
          });
        }

        // Show error message
        if (result.failureCount > 0) {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: `${result.failureCount} item(s) failed to create`
          });
        }

        // If all succeeded, close dialog
        if (result.failureCount === 0) {
          this.saved.emit();
          this.onHide();
        }
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to create items'
        });
        this.saving = false;
      }
    });
  }
}
