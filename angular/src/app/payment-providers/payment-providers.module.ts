import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { PaymentProvidersRoutingModule } from './payment-providers-routing.module';
import { PaymentProvidersManagementComponent } from './payment-providers-management/payment-providers-management.component';

// PrimeNG modules
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputSwitchModule } from 'primeng/inputswitch';
import { ToastModule } from 'primeng/toast';
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@NgModule({
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PaymentProvidersRoutingModule,
    CardModule,
    ButtonModule,
    InputTextModule,
    InputSwitchModule,
    ToastModule,
    ProgressSpinnerModule,
    PaymentProvidersManagementComponent
  ]
})
export class PaymentProvidersModule { }
