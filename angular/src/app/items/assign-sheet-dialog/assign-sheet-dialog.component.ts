import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { MessageService } from 'primeng/api';
import { RentalService } from '../../proxy/rentals/rental.service';
import { ItemSheetService } from '../../proxy/items/item-sheet.service';
import { RentalListDto } from '../../proxy/rentals/models';
import { RentalStatus } from '../../proxy/rentals/rental-status.enum';
import { ItemSheetDto } from '../../proxy/items/models';

@Component({
  selector: 'app-assign-sheet-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DialogModule,
    ButtonModule,
    DropdownModule
  ],
  templateUrl: './assign-sheet-dialog.component.html',
  styleUrl: './assign-sheet-dialog.component.scss'
})
export class AssignSheetDialogComponent implements OnInit {
  @Input() visible = false;
  @Input() itemSheet: ItemSheetDto | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() assigned = new EventEmitter<void>();

  form: FormGroup;
  saving = false;
  rentals: RentalListDto[] = [];
  loadingRentals = false;

  constructor(
    private fb: FormBuilder,
    private rentalService: RentalService,
    private itemSheetService: ItemSheetService,
    private messageService: MessageService
  ) {
    this.form = this.fb.group({
      rentalId: [null, Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadAvailableRentals();
  }

  loadAvailableRentals(): void {
    this.loadingRentals = true;

    // Load active and purchased rentals (not expired)
    this.rentalService.getList({
      status: RentalStatus.Active,
      maxResultCount: 100,
      skipCount: 0
    }).subscribe({
      next: (result) => {
        this.rentals = result.items.filter(r => {
          const endDate = r.endDate ? new Date(r.endDate) : null;
          return endDate && endDate >= new Date();
        });
        this.loadingRentals = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load rentals'
        });
        this.loadingRentals = false;
      }
    });
  }

  onHide(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.form.reset();
  }

  save(): void {
    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        const control = this.form.get(key);
        if (control?.invalid) {
          control.markAsTouched();
          control.markAsDirty();
        }
      });
      return;
    }

    if (!this.itemSheet?.id) {
      return;
    }

    this.saving = true;
    const rentalId = this.form.value.rentalId;

    this.itemSheetService.assignToRental(this.itemSheet.id, { rentalId }).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Item sheet assigned to rental successfully'
        });
        this.assigned.emit();
        this.onHide();
        this.saving = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to assign item sheet to rental'
        });
        this.saving = false;
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (field?.errors) {
      if (field.errors['required']) {
        return 'This field is required';
      }
    }
    return '';
  }

  getRentalLabel(rental: RentalListDto): string {
    const startDate = rental.startDate ? new Date(rental.startDate).toLocaleDateString() : '';
    const endDate = rental.endDate ? new Date(rental.endDate).toLocaleDateString() : '';
    return `Booth ${rental.boothNumber} (${startDate} - ${endDate})`;
  }
}
