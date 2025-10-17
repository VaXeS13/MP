import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../shared/shared.module';
import { PromotionsRoutingModule } from './promotions-routing.module';
import { PromotionListComponent } from './promotion-list/promotion-list.component';
import { PromotionFormComponent } from './promotion-form/promotion-form.component';

// PrimeNG Modules and Components
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { InputTextarea } from 'primeng/inputtextarea';
import { InputNumber } from 'primeng/inputnumber';
import { MultiSelectModule } from 'primeng/multiselect';

@NgModule({
  declarations: [
    PromotionListComponent,
    PromotionFormComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    SharedModule,
    PromotionsRoutingModule,
    // PrimeNG
    TableModule,
    ButtonModule,
    TooltipModule,
    InputTextModule,
    DropdownModule,
    CalendarModule,
    InputTextarea,
    InputNumber,
    MultiSelectModule
  ]
})
export class PromotionsModule { }
