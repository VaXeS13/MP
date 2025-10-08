import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { LocalizationService } from '@abp/ng.core';
import { BoothService } from '../../services/booth.service';
import { CreateBoothDto, Currency } from '../../shared/models/booth.model';

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

  currencyOptions = [
    { label: 'PLN', value: Currency.PLN },
    { label: 'EUR', value: Currency.EUR },
    { label: 'USD', value: Currency.USD },
    { label: 'GBP', value: Currency.GBP },
    { label: 'CZK', value: Currency.CZK }
  ];

  constructor(
    private fb: FormBuilder,
    private boothService: BoothService,
    private messageService: MessageService,
    private localization: LocalizationService
  ) {
    this.boothForm = this.fb.group({
      number: ['', [Validators.required, Validators.maxLength(10)]],
      pricePerDay: [null, [Validators.required, Validators.min(0.01), Validators.max(99999.99)]],
      currency: [Currency.PLN, [Validators.required]]
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

  getCurrencyName(currency: Currency): string {
    switch (currency) {
      case Currency.PLN: return this.localization.instant('::Currency:PLN');
      case Currency.EUR: return this.localization.instant('::Currency:EUR');
      case Currency.USD: return this.localization.instant('::Currency:USD');
      case Currency.GBP: return this.localization.instant('::Currency:GBP');
      case Currency.CZK: return this.localization.instant('::Currency:CZK');
      default: return '';
    }
  }

  getSelectedCurrencyCode(): string {
    const currency = this.boothForm.get('currency')?.value;
    return this.currencyOptions.find(c => c.value === currency)?.label || 'PLN';
  }

  getCurrentLocale(): string {
    return 'pl-PL';
  }


  private markFormGroupTouched(): void {
    Object.keys(this.boothForm.controls).forEach(key => {
      this.boothForm.get(key)?.markAsTouched();
    });
  }
}
