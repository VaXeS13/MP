import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';
import { BoothTypeService } from '../../services/booth-type.service';
import { BoothTypeDto, UpdateBoothTypeDto } from '../../shared/models/booth-type.model';

@Component({
  standalone: false,
  selector: 'app-booth-type-edit',
  templateUrl: './booth-type-edit.component.html',
  styleUrls: ['./booth-type-edit.component.scss']
})
export class BoothTypeEditComponent implements OnInit {
  @Input() boothType!: BoothTypeDto;
  @Output() boothTypeUpdated = new EventEmitter<void>();
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
      commissionPercentage: [null, [Validators.required, Validators.min(0), Validators.max(100)]],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    if (this.boothType) {
      this.boothTypeForm.patchValue({
        name: this.boothType.name,
        description: this.boothType.description,
        commissionPercentage: this.boothType.commissionPercentage,
        isActive: this.boothType.isActive
      });
    }
  }

  onSave(): void {
    if (this.boothTypeForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.saving = true;
    const updateDto: UpdateBoothTypeDto = this.boothTypeForm.value;

    this.boothTypeService.update(this.boothType.id, updateDto).subscribe({
      next: () => {
        this.saving = false;
        this.boothTypeUpdated.emit();
      },
      error: (error) => {
        this.saving = false;
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: this.localization.instant('::BoothType:UpdateError')
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
      case 'isActive': return this.localization.instant('::Common:Status');
      default: return fieldName;
    }
  }
}