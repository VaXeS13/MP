import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ToastModule } from 'primeng/toast';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { PaymentProviderSettingsService, PaymentProviderSettings } from '../services/payment-provider-settings.service';

@Component({
  selector: 'app-payment-providers-management',
  templateUrl: './payment-providers-management.component.html',
  styleUrls: ['./payment-providers-management.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    InputTextModule,
    InputSwitchModule,
    ToastModule,
    ProgressSpinnerModule
  ]
})
export class PaymentProvidersManagementComponent implements OnInit {
  settingsForm: FormGroup;
  loading = false;
  saving = false;

  constructor(
    private fb: FormBuilder,
    private paymentProviderService: PaymentProviderSettingsService,
    private messageService: MessageService
  ) {
    this.settingsForm = this.fb.group({
      przelewy24: this.fb.group({
        enabled: [false],
        merchantId: [''],
        posId: [''],
        apiKey: [''],
        crcKey: ['']
      }),
      payPal: this.fb.group({
        enabled: [false],
        clientId: [''],
        clientSecret: ['']
      }),
      stripe: this.fb.group({
        enabled: [false],
        publishableKey: [''],
        secretKey: [''],
        webhookSecret: ['']
      })
    });

    // Set up conditional validators
    this.setupConditionalValidators();
  }

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.paymentProviderService.getSettings().subscribe({
      next: (settings: PaymentProviderSettings) => {
        this.settingsForm.patchValue(settings);
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading settings:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load payment provider settings'
        });
        this.loading = false;
      }
    });
  }

  onSave(): void {
    if (this.settingsForm.valid) {
      this.saving = true;
      const settings = this.settingsForm.value;

      this.paymentProviderService.updateSettings(settings).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Payment provider settings saved successfully'
          });
          this.saving = false;
        },
        error: (error) => {
          console.error('Error saving settings:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to save payment provider settings'
          });
          this.saving = false;
        }
      });
    } else {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields'
      });
    }
  }

  get przelewy24Form() {
    return this.settingsForm.get('przelewy24') as FormGroup;
  }

  get payPalForm() {
    return this.settingsForm.get('payPal') as FormGroup;
  }

  get stripeForm() {
    return this.settingsForm.get('stripe') as FormGroup;
  }

  get isPrzelewy24Enabled() {
    return this.przelewy24Form.get('enabled')?.value;
  }

  get isPayPalEnabled() {
    return this.payPalForm.get('enabled')?.value;
  }

  get isStripeEnabled() {
    return this.stripeForm.get('enabled')?.value;
  }

  setupConditionalValidators(): void {
    // Set up conditional validators for Przelewy24
    this.przelewy24Form.get('enabled')?.valueChanges.subscribe((enabled: boolean) => {
      const merchantIdControl = this.przelewy24Form.get('merchantId');
      const posIdControl = this.przelewy24Form.get('posId');
      const apiKeyControl = this.przelewy24Form.get('apiKey');
      const crcKeyControl = this.przelewy24Form.get('crcKey');

      if (enabled) {
        merchantIdControl?.setValidators([Validators.required]);
        posIdControl?.setValidators([Validators.required]);
        apiKeyControl?.setValidators([Validators.required]);
        crcKeyControl?.setValidators([Validators.required]);
      } else {
        merchantIdControl?.clearValidators();
        posIdControl?.clearValidators();
        apiKeyControl?.clearValidators();
        crcKeyControl?.clearValidators();
      }

      merchantIdControl?.updateValueAndValidity();
      posIdControl?.updateValueAndValidity();
      apiKeyControl?.updateValueAndValidity();
      crcKeyControl?.updateValueAndValidity();
    });

    // Set up conditional validators for PayPal
    this.payPalForm.get('enabled')?.valueChanges.subscribe((enabled: boolean) => {
      const clientIdControl = this.payPalForm.get('clientId');
      const clientSecretControl = this.payPalForm.get('clientSecret');

      if (enabled) {
        clientIdControl?.setValidators([Validators.required]);
        clientSecretControl?.setValidators([Validators.required]);
      } else {
        clientIdControl?.clearValidators();
        clientSecretControl?.clearValidators();
      }

      clientIdControl?.updateValueAndValidity();
      clientSecretControl?.updateValueAndValidity();
    });

    // Set up conditional validators for Stripe
    this.stripeForm.get('enabled')?.valueChanges.subscribe((enabled: boolean) => {
      const publishableKeyControl = this.stripeForm.get('publishableKey');
      const secretKeyControl = this.stripeForm.get('secretKey');

      if (enabled) {
        publishableKeyControl?.setValidators([Validators.required]);
        secretKeyControl?.setValidators([Validators.required]);
      } else {
        publishableKeyControl?.clearValidators();
        secretKeyControl?.clearValidators();
      }

      publishableKeyControl?.updateValueAndValidity();
      secretKeyControl?.updateValueAndValidity();
    });

    // Trigger initial validation based on current enabled states
    this.przelewy24Form.get('enabled')?.updateValueAndValidity();
    this.payPalForm.get('enabled')?.updateValueAndValidity();
    this.stripeForm.get('enabled')?.updateValueAndValidity();
  }
}
