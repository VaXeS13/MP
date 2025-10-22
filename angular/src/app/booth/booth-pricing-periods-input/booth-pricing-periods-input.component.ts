import { Component, forwardRef, OnInit } from '@angular/core';
import { ControlValueAccessor, FormArray, FormBuilder, FormGroup, NG_VALIDATORS, NG_VALUE_ACCESSOR, ValidationErrors, Validator, Validators } from '@angular/forms';
import { BoothPricingPeriodDto } from '../../proxy/application/contracts/booths/models';

interface PricingPeriodForm {
  enabled: boolean;
  days: number;
  pricePerPeriod: string | number;
}

@Component({
  standalone: false,
  selector: 'app-booth-pricing-periods-input',
  templateUrl: './booth-pricing-periods-input.component.html',
  styleUrl: './booth-pricing-periods-input.component.scss',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => BoothPricingPeriodsInputComponent),
      multi: true
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => BoothPricingPeriodsInputComponent),
      multi: true
    }
  ]
})
export class BoothPricingPeriodsInputComponent implements OnInit, ControlValueAccessor, Validator {
  // Predefined periods: 1, 3, 7, 14, 30 days
  predefinedPeriods = [1, 3, 7, 14, 30];

  periodsForm: FormArray;
  private pendingValue: BoothPricingPeriodDto[] | null = null;
  private isFormInitialized = false;

  private onChange: any = () => {};
  private onTouched: any = () => {};

  constructor(private fb: FormBuilder) {
    this.periodsForm = this.fb.array([]);
    this.initializeForm();
  }

  private initializeForm(): void {
    // Initialize form with predefined periods
    this.predefinedPeriods.forEach(days => {
      const group = this.fb.group({
        enabled: [false],
        days: [days],
        pricePerPeriod: ['', [Validators.min(0.01)]]
      });

      // Subscribe to enabled changes to set/clear validators
      group.get('enabled')?.valueChanges.subscribe(enabled => {
        const priceControl = group.get('pricePerPeriod');
        if (enabled) {
          priceControl?.setValidators([Validators.required, Validators.min(0.01)]);
          priceControl?.markAsTouched();
        } else {
          priceControl?.clearValidators();
          priceControl?.setValue('');
        }
        priceControl?.updateValueAndValidity();
        this.emitValue();
      });

      // Subscribe to price changes
      group.get('pricePerPeriod')?.valueChanges.subscribe(() => {
        this.emitValue();
      });

      this.periodsForm.push(group);
    });

    this.isFormInitialized = true;

    // If there's a pending value that was set via writeValue before form was ready, apply it now
    if (this.pendingValue !== null) {
      this.applyValue(this.pendingValue);
      this.pendingValue = null;
    }
  }

  ngOnInit(): void {
    // Form is already initialized in constructor
  }

  getPeriodGroup(index: number): FormGroup {
    return this.periodsForm.at(index) as FormGroup;
  }

  isPeriodEnabled(index: number): boolean {
    return this.getPeriodGroup(index).get('enabled')?.value || false;
  }

  getPeriodDays(index: number): number {
    return this.getPeriodGroup(index).get('days')?.value || 0;
  }

  getEffectivePricePerDay(index: number): number | null {
    const group = this.getPeriodGroup(index);
    const price = group.get('pricePerPeriod')?.value;
    const days = group.get('days')?.value;

    if (price && days) {
      return price / days;
    }
    return null;
  }

  isFieldInvalid(index: number, fieldName: string): boolean {
    const control = this.getPeriodGroup(index).get(fieldName);
    return control ? control.invalid && control.touched : false;
  }

  private emitValue(): void {
    const enabledPeriods: BoothPricingPeriodDto[] = [];

    this.periodsForm.controls.forEach(group => {
      // Read values DIRECTLY from form controls, not from group.value
      // This avoids race conditions with valueChanges
      const enabledControl = group.get('enabled');
      const priceControl = group.get('pricePerPeriod');
      const daysControl = group.get('days');

      const enabled = enabledControl?.value;
      const price = priceControl?.value;
      const days = daysControl?.value;

      // Only include periods that are:
      // 1. Enabled
      // 2. Have a valid price (not empty string, not null)
      // 3. Price must be a valid number >= 0.01
      if (enabled && price !== null && price !== '' && !isNaN(Number(price))) {
        const priceAsNumber = Number(price);
        // Only include if price is >= 0.01 (minimum valid price)
        if (priceAsNumber >= 0.01) {
          enabledPeriods.push({
            days,
            pricePerPeriod: priceAsNumber
          } as BoothPricingPeriodDto);
        }
      }
    });

    this.onChange(enabledPeriods.length > 0 ? enabledPeriods : null);
    this.onTouched();
  }

  // ControlValueAccessor implementation
  writeValue(value: BoothPricingPeriodDto[] | null): void {
    // If form is not yet initialized, store the value for later application
    if (!this.isFormInitialized) {
      this.pendingValue = value;
      return;
    }

    this.applyValue(value);
  }

  private applyValue(value: BoothPricingPeriodDto[] | null): void {
    if (!value || value.length === 0) {
      // Reset all to disabled
      this.periodsForm.controls.forEach(group => {
        group.patchValue({ enabled: false, pricePerPeriod: '' }, { emitEvent: false });
      });
    } else {
      // Set values from input
      this.periodsForm.controls.forEach(group => {
        const days = group.get('days')?.value;
        const matchingPeriod = value.find(p => p.days === days);

        if (matchingPeriod) {
          group.patchValue({
            enabled: true,
            pricePerPeriod: matchingPeriod.pricePerPeriod
          }, { emitEvent: false });
        } else {
          group.patchValue({
            enabled: false,
            pricePerPeriod: ''
          }, { emitEvent: false });
        }
      });
    }

    // Always emit value after applying to notify parent form
    // This is critical for both initial load and updates
    this.emitValue();
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    if (isDisabled) {
      this.periodsForm.disable();
    } else {
      this.periodsForm.enable();
    }
  }

  // Validator implementation
  validate(): ValidationErrors | null {
    const enabledCount = this.periodsForm.controls
      .filter(group => group.get('enabled')?.value)
      .length;

    if (enabledCount === 0) {
      return { noPeriods: { message: 'At least one pricing period is required' } };
    }

    // Check if any enabled period has invalid price
    const hasInvalidPrice = this.periodsForm.controls.some(group => {
      const enabled = group.get('enabled')?.value;
      const priceControl = group.get('pricePerPeriod');
      return enabled && priceControl?.invalid;
    });

    if (hasInvalidPrice) {
      return { invalidPrices: { message: 'All enabled periods must have valid prices' } };
    }

    return null;
  }
}
