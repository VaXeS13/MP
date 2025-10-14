import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Card } from 'primeng/card';
import { Select } from 'primeng/select';
import { Toast } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { TenantCurrencyService } from '../proxy/tenants/tenant-currency.service';
import { Currency, currencyOptions } from '../proxy/domain/booths/currency.enum';

@Component({
  selector: 'app-tenant-currency-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    Card,
    Select,
    Toast,
  ],
  providers: [MessageService],
  templateUrl: './tenant-currency-settings.component.html',
  styleUrl: './tenant-currency-settings.component.scss',
})
export class TenantCurrencySettingsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  form!: FormGroup;
  currencyOptions = currencyOptions;
  currentCurrency: Currency | null = null;
  isLoading = false;
  isSaving = false;

  constructor(
    private fb: FormBuilder,
    private tenantCurrencyService: TenantCurrencyService,
    private messageService: MessageService
  ) {
    this.createForm();
  }

  ngOnInit(): void {
    this.loadCurrency();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  createForm(): void {
    this.form = this.fb.group({
      currency: [Currency.PLN, [Validators.required]],
    });
  }

  loadCurrency(): void {
    this.isLoading = true;

    this.tenantCurrencyService
      .getTenantCurrency()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.isLoading = false;
          this.currentCurrency = result.currency;
          this.form.patchValue({ currency: result.currency });
        },
        error: error => {
          this.isLoading = false;
          // If tenant currency not set yet, use default PLN
          this.messageService.add({
            severity: 'info',
            summary: 'Info',
            detail: 'Tenant currency not set yet. Using default PLN.',
          });
        },
      });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const currency = this.form.value.currency;

    this.tenantCurrencyService
      .updateTenantCurrency({ currency })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isSaving = false;
          this.currentCurrency = currency;

          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Tenant currency updated successfully',
          });
        },
        error: error => {
          this.isSaving = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.error?.message || 'Failed to update currency',
          });
        },
      });
  }

  getCurrencyName(currency: Currency): string {
    const option = this.currencyOptions.find(o => o.value === currency);
    return option ? option.key : 'Unknown';
  }
}
