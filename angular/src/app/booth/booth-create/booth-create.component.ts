import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';
import { BoothService } from '../../services/booth.service';
import { CreateBoothDto } from '../../shared/models/booth.model';

@Component({
  standalone: false,
  selector: 'app-booth-create',
  templateUrl: './booth-create.component.html',
  styleUrl: './booth-create.component.scss'
})
export class BoothCreateComponent implements OnInit {
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  boothForm: FormGroup;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private boothService: BoothService,
    private messageService: MessageService,
    private localization: LocalizationService
  ) {
    this.boothForm = this.fb.group({
      number: ['', [Validators.required, Validators.maxLength(10)]],
      pricePerDay: [null, [Validators.required, Validators.min(0.01), Validators.max(99999.99)]]
    });
  }

  ngOnInit(): void {}

  onSave(): void {
    if (this.boothForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.saving = true;
    const input: CreateBoothDto = this.boothForm.value;

    this.boothService.create(input).subscribe({
      next: () => {
        this.saving = false;
        this.saved.emit();
      },
      error: (error) => {
        this.saving = false;
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: error.error?.error?.message || this.localization.instant('::Booth:CreateError')
        });
      }
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.boothForm.get(fieldName);
    return field ? field.invalid && (field.dirty || field.touched) : false;
  }

  private markFormGroupTouched(): void {
    Object.keys(this.boothForm.controls).forEach(key => {
      this.boothForm.get(key)?.markAsTouched();
    });
  }
}
