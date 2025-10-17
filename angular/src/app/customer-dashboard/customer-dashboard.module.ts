import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CustomerDashboardRoutingModule } from './customer-dashboard-routing.module';
import { CustomerDashboardComponent } from './customer-dashboard.component';
import { MyItemsComponent } from './my-items/my-items.component';
import { MyRentalsComponent } from './my-rentals/my-rentals.component';
import { SettlementsComponent } from './settlements/settlements.component';

// PrimeNG modules
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { TagModule } from 'primeng/tag';
import { ProgressBarModule } from 'primeng/progressbar';
import { ChartModule } from 'primeng/chart';
import { BadgeModule } from 'primeng/badge';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { MessagesModule } from 'primeng/messages';

import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    CustomerDashboardComponent,
    MyItemsComponent,
    MyRentalsComponent,
    SettlementsComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    CustomerDashboardRoutingModule,
    SharedModule,
    // PrimeNG
    CardModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    DialogModule,
    DropdownModule,
    CalendarModule,
    TagModule,
    ProgressBarModule,
    ChartModule,
    BadgeModule,
    TooltipModule,
    ConfirmDialogModule,
    ToastModule,
    MessagesModule
  ]
})
export class CustomerDashboardModule { }
