import { Component, EventEmitter, Input, OnInit, Output, OnChanges, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';
import { BoothService } from '../../proxy/booths/booth.service';
import { BoothListDto, UpdateBoothDto } from '../../proxy/booths/models';
import { BoothStatus } from '../../proxy/domain/booths/booth-status.enum';

@Component({
  standalone: false,
  selector: 'app-booth-edit',
  templateUrl: './booth-edit.component.html',
  styleUrl: './booth-edit.component.scss'
})
export class BoothEditComponent implements OnInit, OnChanges {
  @Input() booth!: BoothListDto;
  @Output() saved = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  boothForm: FormGroup;
  saving = false;
  statusOptions: any[] = [];

  constructor(
    private fb: FormBuilder,
    private boothService: BoothService,
    private messageService: MessageService,
    private localization: LocalizationService
  ) {
    this.boothForm = this.fb.group({
      number: ['', [Validators.required, Validators.maxLength(10)]],
      pricingPeriods: [null, [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.loadBoothData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['booth'] && !changes['booth'].firstChange) {
      // Booth input changed after initial load
      this.loadBoothData();
    }
  }

  private loadBoothData(): void {
    if (!this.booth) {
      return;
    }

    // Reset form and set booth data
    this.boothForm.reset();
    this.boothForm.patchValue({
      number: this.booth.number || '',
      pricingPeriods: this.booth.pricingPeriods || []
    });
  }

  onSave(): void {
    if (this.boothForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.saving = true;
    const input: UpdateBoothDto = {
      ...this.boothForm.value,
      status: this.booth.status // Preserve existing status
    };

    this.boothService.update(this.booth.id, input).subscribe({
      next: () => {
        this.saving = false;
        this.saved.emit();
      },
      error: (error) => {
        this.saving = false;
        this.messageService.add({
          severity: 'error',
          summary: this.localization.instant('::Messages:Error'),
          detail: error.error?.error?.message || this.localization.instant('::Booth:UpdateError')
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
