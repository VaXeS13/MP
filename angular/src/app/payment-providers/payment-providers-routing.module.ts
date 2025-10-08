import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PaymentProvidersManagementComponent } from './payment-providers-management/payment-providers-management.component';

const routes: Routes = [
  {
    path: '',
    component: PaymentProvidersManagementComponent,
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PaymentProvidersRoutingModule { }
