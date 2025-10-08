import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { CoreModule } from '@abp/ng.core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { DialogModule } from 'primeng/dialog';
import { PaginatorModule } from 'primeng/paginator';
import { TableModule } from 'primeng/table';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService, ConfirmationService } from 'primeng/api';

import { RentalRoutingModule } from './rental-routing.module';
import { RentalCalendarComponent } from './rental-calendar/rental-calendar.component';
import { RentalListComponent } from './rental-list/rental-list.component';
import { RentalBoothSelectionComponent } from './rental-booth-selection/rental-booth-selection.component';
import { PaymentSelectionComponent } from './payment-selection/payment-selection.component';
import { RentalDetailsComponent } from './rental-details/rental-details.component';

@NgModule({
  declarations: [
    RentalCalendarComponent,
    RentalListComponent,
    RentalBoothSelectionComponent,
    PaymentSelectionComponent,
    RentalDetailsComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    NgbModule,
    CoreModule,
    ThemeSharedModule,
    RentalRoutingModule,
    DialogModule,
    PaginatorModule,
    TableModule,
    ConfirmDialogModule,
    ToastModule,
    CheckboxModule
  ],
  providers: [
    MessageService,
    ConfirmationService
  ]
})
export class RentalModule { }