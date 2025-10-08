import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { ToastModule } from 'primeng/toast';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ItemSheetService, ItemService } from '@proxy/items';
import type { ItemSheetDto, ItemDto, AddItemToSheetDto, AssignSheetToRentalDto } from '@proxy/items/models';
import { RentalService } from '@proxy/rentals';
import type { RentalListDto } from '@proxy/rentals/models';
import { RentalStatus } from '@proxy/rentals/rental-status.enum';

@Component({
  standalone: true,
  selector: 'app-item-sheet-detail',
  templateUrl: './item-sheet-detail.component.html',
  styleUrls: ['./item-sheet-detail.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    TableModule,
    DialogModule,
    DropdownModule,
    ToastModule
  ],
  providers: [MessageService]
})
export class ItemSheetDetailComponent implements OnInit {
  sheetId: string | null = null;
  sheet: ItemSheetDto | null = null;
  loading = false;

  // Add item dialog
  displayAddItemDialog = false;
  availableItems: ItemDto[] = [];
  selectedItemId: string | null = null;

  // Assign to rental dialog
  displayAssignDialog = false;
  availableRentals: RentalListDto[] = [];
  selectedRentalId: string | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private itemSheetService: ItemSheetService,
    private itemService: ItemService,
    private rentalService: RentalService,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.sheetId = params.get('id');
      if (this.sheetId) {
        this.loadSheet();
      }
    });
  }

  loadSheet(): void {
    if (!this.sheetId) return;

    this.loading = true;
    this.itemSheetService.get(this.sheetId).subscribe({
      next: (sheet) => {
        this.sheet = sheet;
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load sheet'
        });
        this.loading = false;
      }
    });
  }

  openAddItemDialog(): void {
    this.loading = true;
    this.itemService.getMyItems({ skipCount: 0, maxResultCount: 100 }).subscribe({
      next: (result) => {
        // Filter only Draft items
        this.availableItems = (result.items || []).filter(i => i.status === 'Draft');
        this.displayAddItemDialog = true;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load items'
        });
        this.loading = false;
      }
    });
  }

  addItemToSheet(): void {
    if (!this.sheetId || !this.selectedItemId) return;

    const dto: AddItemToSheetDto = {
      itemId: this.selectedItemId,
      commissionPercentage: 0 // Will be set from BoothType when sheet is assigned to rental
    };

    this.itemSheetService.addItemToSheet(this.sheetId, dto).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Item added to sheet'
        });
        this.displayAddItemDialog = false;
        this.selectedItemId = null;
        this.loadSheet();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to add item'
        });
      }
    });
  }

  removeItemFromSheet(itemId: string): void {
    if (!this.sheetId) return;

    this.itemSheetService.removeItemFromSheet(this.sheetId, itemId).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Item removed from sheet'
        });
        this.loadSheet();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to remove item'
        });
      }
    });
  }

  openAssignDialog(): void {
    this.loading = true;
    this.rentalService.getMyRentals({
      skipCount: 0,
      maxResultCount: 100,
      status: undefined
    }).subscribe({
      next: (result) => {
        // Filter only Active and Extended rentals that haven't ended
        const now = new Date();
        this.availableRentals = (result.items || []).filter(r => {
          const endDate = r.endDate ? new Date(r.endDate) : null;
          const isActive = r.status === RentalStatus.Active || r.status === RentalStatus.Extended;
          const notEnded = endDate ? endDate >= now : false;
          return isActive && notEnded;
        });
        this.displayAssignDialog = true;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load rentals'
        });
        this.loading = false;
      }
    });
  }

  assignToRental(): void {
    if (!this.sheetId || !this.selectedRentalId) return;

    const dto: AssignSheetToRentalDto = {
      rentalId: this.selectedRentalId
    };

    this.itemSheetService.assignToRental(this.sheetId, dto).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Sheet assigned to rental'
        });
        this.displayAssignDialog = false;
        this.selectedRentalId = null;
        this.loadSheet();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to assign sheet'
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/items/sheets']);
  }

  getCommissionAmount(sheetItem: any): number {
    if (!sheetItem.item?.price || !sheetItem.commissionPercentage) {
      return 0;
    }
    return (sheetItem.item.price * sheetItem.commissionPercentage) / 100;
  }
}
