import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';
import { BoothTypeService } from '../../services/booth-type.service';
import { CreateBoothTypeDto } from '../../shared/models/booth-type.model';

@Component({
  standalone: false,
  selector: 'app-booth-type-create',
  templateUrl: './booth-type-create.component.html',
  styleUrls: ['./booth-type-create.component.scss']
})
export class BoothTypeCreateComponent implements OnInit {
  @Output() boothTypeCreated = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  boothTypeForm: FormGroup;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private boothTypeService: BoothTypeService,
    private messageService: MessageService,
    private localization: LocalizationService
  ) {
    this.boothTypeForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.required, Validators.maxLength(500)]],
      commissionPercentage: [null, [Validators.required, Validators.min(0), Validators.max(100)]]
    });
  }

  ngOnInit(): void {}

  onSave(): void {
    if (this.boothTypeForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.saving = true;
    const createDto: CreateBoothTypeDto = this.boothTypeForm.value;

    this.boothTypeService.create(createDto).subscribe({
      next: () => {
        this.saving = false;
        this.boothTypeCreated.emit();
      },
      error: (error) => {
        this.saving = false;
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: this.localization.instant('::BoothType:CreateError')
        });
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  private markFormGroupTouched(): void {
    Object.keys(this.boothTypeForm.controls).forEach(key => {
      const control = this.boothTypeForm.get(key);
      control?.markAsTouched();
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.boothTypeForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.boothTypeForm.get(fieldName);
    if (field && field.errors && (field.dirty || field.touched)) {
      const fieldLabel = this.getFieldLabel(fieldName);
      if (field.errors['required']) {
        return `${fieldLabel} ${this.localization.instant('::Validation:Required')}`;
      }
      if (field.errors['maxlength']) {
        const maxLength = field.errors['maxlength'].requiredLength;
        return `${fieldLabel} ${this.localization.instant('::Validation:MaxLength', maxLength)}`;
      }
      if (field.errors['min']) {
        return `${fieldLabel} ${this.localization.instant('::Validation:MinValue', field.errors['min'].min)}`;
      }
      if (field.errors['max']) {
        return `${fieldLabel} ${this.localization.instant('::Validation:MaxValue', field.errors['max'].max)}`;
      }
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    switch (fieldName) {
      case 'name': return this.localization.instant('::BoothType:Name');
      case 'description': return this.localization.instant('::BoothType:Description');
      case 'commissionPercentage': return this.localization.instant('::BoothType:Commission');
      default: return fieldName;
    }
  }
}