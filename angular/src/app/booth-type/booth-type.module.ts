import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CoreModule } from '@abp/ng.core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { InputTextarea } from 'primeng/inputtextarea';
import { InputNumber } from 'primeng/inputnumber';
import { Checkbox } from 'primeng/checkbox';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';
import { Dialog } from 'primeng/dialog';
import { Toast } from 'primeng/toast';
import { Paginator } from 'primeng/paginator';

import { BoothTypeRoutingModule } from './booth-type-routing.module';
import { BoothTypeListComponent } from './booth-type-list/booth-type-list.component';
import { BoothTypeCreateComponent } from './booth-type-create/booth-type-create.component';
import { BoothTypeEditComponent } from './booth-type-edit/booth-type-edit.component';

@NgModule({
  declarations: [
    BoothTypeListComponent,
    BoothTypeCreateComponent,
    BoothTypeEditComponent
  ],
  imports: [
    CommonModule,
    CoreModule,
    ReactiveFormsModule,
    FormsModule,
    BoothTypeRoutingModule,
    ButtonModule,
    TableModule,
    InputTextModule,
    InputTextarea,
    InputNumber,
    Checkbox,
    ConfirmDialog,
    Tag,
    Tooltip,
    Dialog,
    Toast,
    Paginator
  ]
})
export class BoothTypeModule { }