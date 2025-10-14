import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { DialogModule } from 'primeng/dialog';
import { PaginatorModule } from 'primeng/paginator';
import { TableModule } from 'primeng/table';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { CheckboxModule } from 'primeng/checkbox';
import { CalendarModule } from 'primeng/calendar';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { MessageService, ConfirmationService } from 'primeng/api';

import { RentalRoutingModule } from './rental-routing.module';
import { RentalSharedModule } from './rental-shared.module';
import { RentalListComponent } from './rental-list/rental-list.component';
import { RentalBoothSelectionComponent } from './rental-booth-selection/rental-booth-selection.component';
import { RentalDetailsComponent } from './rental-details/rental-details.component';
import { RentalCalendarPageComponent } from './rental-calendar-page/rental-calendar-page.component';

@NgModule({
  declarations: [
    RentalListComponent,
    RentalBoothSelectionComponent,
    RentalDetailsComponent,
    RentalCalendarPageComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModule,
    CoreModule,
    ThemeSharedModule,
    RentalRoutingModule,
    RentalSharedModule,
    DialogModule,
    PaginatorModule,
    TableModule,
    ConfirmDialogModule,
    ToastModule,
    CheckboxModule,
    CalendarModule,
    RadioButtonModule,
    InputNumberModule,
    MessageModule,
    InputTextModule,
    ButtonModule
  ],
  providers: [
    MessageService,
    ConfirmationService
  ],
  exports: [
    RentalSharedModule
  ]
})
export class RentalModule { }