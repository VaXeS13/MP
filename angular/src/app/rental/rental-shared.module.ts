import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CoreModule } from '@abp/ng.core';

// PrimeNG
import { DialogModule } from 'primeng/dialog';
import { CalendarModule } from 'primeng/calendar';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';

// Components
import { ExtendRentalDialogComponent } from './extend-rental-dialog/extend-rental-dialog.component';

@NgModule({
  declarations: [
    ExtendRentalDialogComponent
  ],
  imports: [
    CommonModule,
    CoreModule,
    FormsModule,
    ReactiveFormsModule,
    DialogModule,
    CalendarModule,
    RadioButtonModule,
    InputNumberModule,
    MessageModule,
    InputTextModule,
    ButtonModule
  ],
  exports: [
    ExtendRentalDialogComponent,
    DialogModule,
    CalendarModule,
    RadioButtonModule,
    InputNumberModule,
    MessageModule,
    InputTextModule,
    ButtonModule
  ]
})
export class RentalSharedModule { }
