import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CustomerDashboardRoutingModule } from './customer-dashboard-routing.module';
import { CustomerDashboardComponent } from './customer-dashboard.component';
import { MyItemsComponent } from './my-items/my-items.component';
import { MyRentalsComponent } from './my-rentals/my-rentals.component';
import { MyRentalDetailComponent } from './my-rental-detail/my-rental-detail.component';
import { SettlementsComponent } from './settlements/settlements.component';

// PrimeNG modules
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { TagModule } from 'primeng/tag';
import { ProgressBarModule } from 'primeng/progressbar';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ChartModule } from 'primeng/chart';
import { BadgeModule } from 'primeng/badge';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { MessagesModule } from 'primeng/messages';
import { TabViewModule } from 'primeng/tabview';
import { PanelModule } from 'primeng/panel';
import { DataViewModule } from 'primeng/dataview';
import { RippleModule } from 'primeng/ripple';

import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    CustomerDashboardComponent,
    MyItemsComponent,
    MyRentalsComponent,
    MyRentalDetailComponent,
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
    TextareaModule,
    DialogModule,
    DropdownModule,
    CalendarModule,
    TagModule,
    ProgressBarModule,
    ProgressSpinnerModule,
    ChartModule,
    BadgeModule,
    TooltipModule,
    ConfirmDialogModule,
    ToastModule,
    MessagesModule,
    TabViewModule,
    PanelModule,
    DataViewModule,
    RippleModule
  ]
})
export class CustomerDashboardModule { }
