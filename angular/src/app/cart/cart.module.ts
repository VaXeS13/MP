import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CoreModule } from '@abp/ng.core';

// PrimeNG Modules
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageModule } from 'primeng/message';
import { DialogModule } from 'primeng/dialog';
import { ConfirmationService } from 'primeng/api';

// Rental Shared Module
import { RentalSharedModule } from '../rental/rental-shared.module';

// Components
import { CartIconComponent } from './cart-icon/cart-icon.component';
import { CartComponent } from './cart/cart.component';
import { CheckoutComponent } from './checkout/checkout.component';
import { EditCartItemDialogComponent } from './edit-cart-item-dialog/edit-cart-item-dialog.component';

const routes: Routes = [
  {
    path: '',
    component: CartComponent
  },
  {
    path: 'checkout',
    component: CheckoutComponent
  }
];

@NgModule({
  declarations: [
    CartIconComponent,
    CartComponent,
    CheckoutComponent,
    EditCartItemDialogComponent
  ],
  imports: [
    CommonModule,
    CoreModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forChild(routes),
    RentalSharedModule,
    ButtonModule,
    CardModule,
    TableModule,
    TooltipModule,
    ProgressSpinnerModule,
    ConfirmDialogModule,
    MessageModule,
    DialogModule
  ],
  providers: [ConfirmationService],
  exports: [
    CartIconComponent  // Export so it can be used in layouts
  ]
})
export class CartModule { }