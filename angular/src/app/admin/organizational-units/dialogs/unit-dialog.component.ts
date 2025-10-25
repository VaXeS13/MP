import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { OrganizationalUnitService } from '@services/organizational-unit.service';
import { OrganizationalUnitDto, CreateUpdateOrganizationalUnitDto } from '@proxy/organizational-units/dtos/models';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-unit-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, PrimeNGModule],
  templateUrl: './unit-dialog.component.html',
  styleUrls: ['./unit-dialog.component.scss'],
  providers: [MessageService],
})
export class UnitDialogComponent implements OnInit {
  @Input() unit: OrganizationalUnitDto | null = null;
  @Input() isEditMode = false;
  @Output() save = new EventEmitter<OrganizationalUnitDto>();
  @Output() close = new EventEmitter<void>();

  form!: FormGroup;
  isLoading = false;

  constructor(
    private fb: FormBuilder,
    private orgUnitService: OrganizationalUnitService,
    private messageService: MessageService
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    if (this.isEditMode && this.unit) {
      this.form.patchValue(this.unit);
    }
  }

  /**
   * Initialize form with validation rules
   */
  private initializeForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(200)]],
      code: ['', [Validators.required, Validators.maxLength(50)]],
      address: ['', Validators.maxLength(255)],
      city: ['', Validators.maxLength(100)],
      postalCode: ['', Validators.maxLength(20)],
      email: ['', [Validators.email]],
      phone: ['', Validators.maxLength(20)],
      isActive: [true],
    });
  }

  /**
   * Generate code from unit name
   */
  generateCode(): void {
    const name = this.form.get('name')?.value;
    if (name) {
      const code = name
        .toUpperCase()
        .replace(/[^A-Z0-9]/g, '')
        .substring(0, 50);
      this.form.patchValue({ code });
    }
  }

  /**
   * Submit form - create or update unit
   */
  submitForm(): void {
    if (!this.form.valid) {
      this.markFormGroupTouched(this.form);
      return;
    }

    const formValue: CreateUpdateOrganizationalUnitDto = this.form.value;
    this.isLoading = true;

    const request = this.isEditMode && this.unit?.id
      ? this.orgUnitService.updateUnit(this.unit.id, formValue)
      : this.orgUnitService.createUnit(formValue);

    request.subscribe({
      next: (result) => {
        this.isLoading = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: this.isEditMode ? 'Unit updated successfully' : 'Unit created successfully',
        });
        this.save.emit(result);
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Failed to save unit', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.message || 'Failed to save unit',
        });
      },
    });
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.close.emit();
  }

  /**
   * Check if field has error
   */
  hasError(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  /**
   * Mark all fields as touched to show validation errors
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach((key) => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
}
