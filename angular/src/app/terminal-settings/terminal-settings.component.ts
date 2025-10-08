import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Card } from 'primeng/card';
import { InputText } from 'primeng/inputtext';
import { InputSwitch } from 'primeng/inputswitch';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/inputtextarea';
import { Toast } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { TerminalSettingsService } from '../proxy/application/terminals/terminal-settings.service';
import {
  TerminalSettingsDto,
  TerminalProviderInfoDto,
} from '../proxy/application/contracts/terminals/models';

@Component({
  selector: 'app-terminal-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    Card,
    InputText,
    InputSwitch,
    Select,
    Textarea,
    Toast,
    ConfirmDialog,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './terminal-settings.component.html',
  styleUrl: './terminal-settings.component.scss',
})
export class TerminalSettingsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  form!: FormGroup;
  settings: TerminalSettingsDto | null = null;
  providers: TerminalProviderInfoDto[] = [];
  isLoading = false;
  isSaving = false;

  constructor(
    private fb: FormBuilder,
    private terminalSettingsService: TerminalSettingsService,
    private messageService: MessageService,
    private confirmationService: ConfirmationService
  ) {
    this.createForm();
  }

  ngOnInit(): void {
    this.loadProviders();
    this.loadSettings();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  createForm(): void {
    this.form = this.fb.group({
      providerId: ['mock', [Validators.required, Validators.maxLength(50)]],
      isEnabled: [true],
      configurationJson: ['{}', [Validators.required, Validators.maxLength(4000)]],
      currency: ['PLN', [Validators.required, Validators.maxLength(3)]],
      region: [''],
      isSandbox: [true],
    });
  }

  loadProviders(): void {
    this.terminalSettingsService
      .getAvailableProviders()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: providers => {
          this.providers = providers;
        },
        error: error => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load terminal providers',
          });
        },
      });
  }

  loadSettings(): void {
    this.isLoading = true;

    this.terminalSettingsService
      .getCurrentTenantSettings()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: settings => {
          this.isLoading = false;
          this.settings = settings;

          if (settings) {
            this.form.patchValue({
              providerId: settings.providerId,
              isEnabled: settings.isEnabled,
              configurationJson: settings.configurationJson,
              currency: settings.currency,
              region: settings.region,
              isSandbox: settings.isSandbox,
            });
          }
        },
        error: error => {
          this.isLoading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load terminal settings',
          });
        },
      });
  }

  save(): void {
    if (this.form.invalid) {
      Object.keys(this.form.controls).forEach(key => {
        this.form.controls[key].markAsTouched();
      });
      return;
    }

    this.isSaving = true;
    const formValue = this.form.value;

    const request = this.settings
      ? this.terminalSettingsService.update(this.settings.id, formValue)
      : this.terminalSettingsService.create(formValue);

    request.pipe(takeUntil(this.destroy$)).subscribe({
      next: result => {
        this.isSaving = false;
        this.settings = result;

        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: this.settings ? 'Settings updated successfully' : 'Settings created successfully',
        });
      },
      error: error => {
        this.isSaving = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error.error?.error?.message || 'Failed to save settings',
        });
      },
    });
  }

  delete(): void {
    if (!this.settings) {
      return;
    }

    this.confirmationService.confirm({
      message: 'Are you sure you want to delete terminal settings?',
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.terminalSettingsService
          .delete(this.settings!.id)
          .pipe(takeUntil(this.destroy$))
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'Settings deleted successfully',
              });

              this.settings = null;
              this.createForm();
            },
            error: error => {
              this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to delete settings',
              });
            },
          });
      },
    });
  }

  validateJson(): void {
    const control = this.form.get('configurationJson');
    if (!control) return;

    try {
      JSON.parse(control.value);
      control.setErrors(null);
    } catch (e) {
      control.setErrors({ invalidJson: true });
    }
  }

  trackByProviderId(index: number, provider: TerminalProviderInfoDto): string {
    return provider.providerId;
  }
}