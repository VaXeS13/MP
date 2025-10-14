import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { RouterModule } from '@angular/router';

// PrimeNG
import { DialogModule } from 'primeng/dialog';
import { CalendarModule } from 'primeng/calendar';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

// Components
import { ExtendRentalDialogComponent } from './extend-rental-dialog/extend-rental-dialog.component';
import { RentalCalendarComponent } from './rental-calendar/rental-calendar.component';
import { PaymentSelectionComponent } from './payment-selection/payment-selection.component';

@NgModule({
  declarations: [
    ExtendRentalDialogComponent,
    RentalCalendarComponent,
    PaymentSelectionComponent
  ],
  imports: [
    CommonModule,
    CoreModule,
    ThemeSharedModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    DialogModule,
    CalendarModule,
    RadioButtonModule,
    InputNumberModule,
    MessageModule,
    InputTextModule,
    ButtonModule,
    TooltipModule
  ],
  exports: [
    ExtendRentalDialogComponent,
    RentalCalendarComponent,
    DialogModule,
    CalendarModule,
    RadioButtonModule,
    InputNumberModule,
    MessageModule,
    InputTextModule,
    ButtonModule,
    TooltipModule,
    ThemeSharedModule,
    CommonModule,
    FormsModule,
    ReactiveFormsModule
  ]
})
export class RentalSharedModule { }
