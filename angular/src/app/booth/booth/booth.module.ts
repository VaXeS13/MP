import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { CoreModule } from '@abp/ng.core';

// PrimeNG
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { ToolbarModule } from 'primeng/toolbar';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { CalendarModule } from 'primeng/calendar';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageModule } from 'primeng/message';
import { BoothListComponent } from '../booth-list/booth-list.component';
import { BoothCreateComponent } from '../booth-create/booth-create.component';
import { BoothEditComponent } from '../booth-edit/booth-edit.component';
import { BoothSettingsComponent } from '../booth-settings/booth-settings.component';
import { AdminBoothRentalDialogComponent } from '../admin-booth-rental-dialog/admin-booth-rental-dialog.component';
import { RentalSharedModule } from '../../rental/rental-shared.module';
// Components


const routes: Routes = [
  {
    path: '',
    component: BoothListComponent
  },
  {
    path: 'list',
    component: BoothListComponent
  },
  {
    path: 'settings',
    component: BoothSettingsComponent
  }
];

@NgModule({
  declarations: [
    BoothListComponent,
    BoothCreateComponent,
    BoothEditComponent,
    BoothSettingsComponent,
    AdminBoothRentalDialogComponent
  ],
  imports: [
    CommonModule,
    CoreModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forChild(routes),

    // PrimeNG
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    DropdownModule,
    SelectModule,
    InputNumberModule,
    TagModule,
    CardModule,
    ToolbarModule,
    ConfirmDialogModule,
    ToastModule,
    TooltipModule,
    ProgressSpinnerModule,
    CalendarModule,
    DatePickerModule,
    MessageModule,

    // Rental Shared Module (for ExtendRentalDialogComponent)
    RentalSharedModule
  ]
})
export class BoothModule { }