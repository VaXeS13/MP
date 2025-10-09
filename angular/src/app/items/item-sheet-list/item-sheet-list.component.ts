import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { BadgeModule } from 'primeng/badge';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ItemSheetService } from '@proxy/items';
import type { ItemSheetDto, CreateItemSheetDto } from '@proxy/items/models';
import { BarcodeLabelGeneratorService } from '../services/barcode-label-generator.service';
import { AssignSheetDialogComponent } from '../assign-sheet-dialog/assign-sheet-dialog.component';

@Component({
  standalone: true,
  selector: 'app-item-sheet-list',
  templateUrl: './item-sheet-list.component.html',
  styleUrls: ['./item-sheet-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    DialogModule,
    BadgeModule,
    ToastModule,
    ConfirmDialogModule,
    AssignSheetDialogComponent
  ],
  providers: [MessageService, ConfirmationService]
})
export class ItemSheetListComponent implements OnInit {
  sheets: ItemSheetDto[] = [];
  totalCount = 0;
  loading = false;

  // Pagination
  first = 0;
  rows = 10;

  // Dialog
  displayCreateDialog = false;
  displayAssignDialog = false;
  selectedSheet: ItemSheetDto | null = null;

  constructor(
    private itemSheetService: ItemSheetService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService,
    private router: Router,
    private barcodeLabelGenerator: BarcodeLabelGeneratorService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadSheets();
  }

  loadSheets(): void {
    this.loading = true;

    this.itemSheetService.getMyItemSheets({
      skipCount: this.first,
      maxResultCount: this.rows
    }).subscribe({
      next: (result) => {
        this.sheets = result.items || [];
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
    this.loadSheets();
  }

  openCreateDialog(): void {
    this.displayCreateDialog = true;
  }

  closeCreateDialog(): void {
    this.displayCreateDialog = false;
  }

  createSheet(): void {
    const dto: CreateItemSheetDto = {};

    this.itemSheetService.create(dto).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Sheet created successfully'
        });
        this.displayCreateDialog = false;
        this.loadSheets();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to create sheet'
        });
        this.cdr.markForCheck();
      }
    });
  }

  viewSheet(sheet: ItemSheetDto): void {
    if (sheet.id) {
      this.router.navigate(['/items/sheets', sheet.id]);
    }
  }

  deleteSheet(sheet: ItemSheetDto): void {
    this.confirmationService.confirm({
      message: 'Are you sure you want to delete this sheet?',
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        if (sheet.id) {
          this.itemSheetService.delete(sheet.id).subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Sheet deleted successfully'
              });
              this.loadSheets();
            },
            error: (error) => {
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error.error?.error?.message || 'Failed to delete sheet'
              });
            }
          });
        }
      }
    });
  }

  generateBarcodes(sheet: ItemSheetDto): void {
    if (sheet.id) {
      this.itemSheetService.generateBarcodes(sheet.id).subscribe({
        next: (updatedSheet) => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Barcodes generated successfully'
          });

          // Generate PDF labels
          this.barcodeLabelGenerator.generateLabels(updatedSheet);

          this.loadSheets();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.error?.message || 'Failed to generate barcodes'
          });
          this.cdr.markForCheck();
        }
      });
    }
  }

  openAssignDialog(sheet: ItemSheetDto): void {
    this.selectedSheet = sheet;
    this.displayAssignDialog = true;
  }

  onSheetAssigned(): void {
    this.loadSheets();
  }
}
