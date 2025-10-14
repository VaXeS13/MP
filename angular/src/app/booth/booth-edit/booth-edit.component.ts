import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';
import { BoothService } from '../../services/booth.service';
import { BoothListDto, UpdateBoothDto, BoothStatus } from '../../shared/models/booth.model';

@Component({
  standalone: false,
  selector: 'app-booth-edit',
  templateUrl: './booth-edit.component.html',
  styleUrl: './booth-edit.component.scss'
})
export class BoothEditComponent implements OnInit {
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
      pricePerDay: [null, [Validators.required, Validators.min(0.01), Validators.max(9999.99)]],
      status: [null, [Validators.required]]
    });
  }

  ngOnInit(): void {
    this.statusOptions = [
      { label: this.localization.instant('::Status:Available'), value: BoothStatus.Available },
      { label: this.localization.instant('::Status:Maintenance'), value: BoothStatus.Maintenance }
    ];

    this.boothForm.patchValue({
      number: this.booth.number,
      pricePerDay: this.booth.pricePerDay,
      status: this.booth.status
    });

    // Jeśli stanowisko zarezerwowane lub wynajęte, nie pozwalaj zmienić statusu
    if (this.booth.status === BoothStatus.Reserved || this.booth.status === BoothStatus.Rented) {
      this.boothForm.get('status')?.disable();
    }
  }

  onSave(): void {
    if (this.boothForm.invalid) {
      this.markFormGroupTouched();
      return;
    }

    this.saving = true;
    const input: UpdateBoothDto = {
      ...this.boothForm.value,
      status: (this.booth.status === BoothStatus.Reserved || this.booth.status === BoothStatus.Rented) ? this.booth.status : this.boothForm.value.status
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
