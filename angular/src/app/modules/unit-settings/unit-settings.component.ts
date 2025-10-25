import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TabViewModule } from 'primeng/tabview';
import { PrimeNGModule } from '@shared/prime-ng.module';
import { OrganizationalUnitService } from '@services/organizational-unit.service';
import { CurrentOrganizationalUnitService } from '@services/current-organizational-unit.service';
import { UpdateUnitSettingsDto } from '@proxy/organizational-units/dtos/models';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-unit-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TabViewModule, PrimeNGModule],
  templateUrl: './unit-settings.component.html',
  styleUrls: ['./unit-settings.component.scss'],
  providers: [MessageService],
})
export class UnitSettingsComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  isLoading = false;
  isSaving = false;
  currentUnitId: string | null = null;
  paymentProviders: { name: string; label: string }[] = [
    { name: 'stripe', label: 'Stripe' },
    { name: 'przelewy24', label: 'Przelewy24' },
    { name: 'paypal', label: 'PayPal' },
  ];

  currencies = ['PLN', 'EUR', 'USD', 'GBP', 'CZK'];

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private orgUnitService: OrganizationalUnitService,
    private currentUnitService: CurrentOrganizationalUnitService,
    private messageService: MessageService
  ) {
    this.initializeForm();
  }

  ngOnInit(): void {
    const currentUnit = this.currentUnitService.getCurrentUnit();
    if (currentUnit?.unitId) {
      this.currentUnitId = currentUnit.unitId;
      this.loadUnitSettings();
    }
  }

  /**
   * Initialize form with controls
   */
  private initializeForm(): void {
    this.form = this.fb.group({
      currency: ['PLN', Validators.required],
      enabledPaymentProviders: [{}],
      defaultPaymentProvider: ['stripe'],
      logoUrl: [''],
      bannerText: ['', Validators.maxLength(500)],
    });
  }

  /**
   * Load unit settings from API
   */
  loadUnitSettings(): void {
    if (!this.currentUnitId) return;

    this.isLoading = true;
    this.orgUnitService
      .getUnitSettings(this.currentUnitId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (settings) => {
          if (settings) {
            this.form.patchValue({
              currency: settings.currency || 'PLN',
              enabledPaymentProviders: settings.enabledPaymentProviders || {},
              defaultPaymentProvider: settings.defaultPaymentProvider || 'stripe',
              logoUrl: settings.logoUrl || '',
              bannerText: settings.bannerText || '',
            });
          }
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Failed to load settings', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load unit settings',
          });
          this.isLoading = false;
        },
      });
  }

  /**
   * Save unit settings
   */
  saveSettings(): void {
    if (!this.form.valid || !this.currentUnitId) return;

    this.isSaving = true;
    const input: UpdateUnitSettingsDto = this.form.value;

    this.orgUnitService
      .updateUnitSettings(this.currentUnitId, input)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isSaving = false;
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Unit settings saved successfully',
          });
        },
        error: (error) => {
          this.isSaving = false;
          console.error('Failed to save settings', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error.error?.message || 'Failed to save settings',
          });
        },
      });
  }

  /**
   * Toggle payment provider enabled status
   */
  toggleProvider(providerName: string): void {
    const providers = this.form.get('enabledPaymentProviders')?.value || {};
    providers[providerName] = !providers[providerName];
    this.form.patchValue({ enabledPaymentProviders: providers });
  }

  /**
   * Check if payment provider is enabled
   */
  isProviderEnabled(providerName: string): boolean {
    const providers = this.form.get('enabledPaymentProviders')?.value || {};
    return providers[providerName] === true;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
