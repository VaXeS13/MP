import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '../shared/shared.module';
import { CoreModule } from '@abp/ng.core';
import { HomepageContentRoutingModule } from './homepage-content-routing.module';
import { HomepageSectionListComponent } from './homepage-section-list/homepage-section-list.component';
import { HomepageSectionFormComponent } from './homepage-section-form/homepage-section-form.component';

// PrimeNG Modules
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { InputTextarea } from 'primeng/inputtextarea';
import { ColorPickerModule } from 'primeng/colorpicker';
import { InputSwitchModule } from 'primeng/inputswitch';

@NgModule({
  declarations: [
    HomepageSectionListComponent,
    HomepageSectionFormComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    SharedModule,
    CoreModule,
    HomepageContentRoutingModule,
    // PrimeNG
    TableModule,
    ButtonModule,
    TooltipModule,
    InputTextModule,
    DropdownModule,
    CalendarModule,
    InputTextarea,
    ColorPickerModule,
    InputSwitchModule
  ]
})
export class HomepageContentModule { }
