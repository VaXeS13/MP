import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';

// PrimeNG Modules
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { CalendarModule } from 'primeng/calendar';
import { CheckboxModule } from 'primeng/checkbox';
import { SelectButtonModule } from 'primeng/selectbutton';
import { DropdownModule } from 'primeng/dropdown';
import { BadgeModule } from 'primeng/badge';
import { DividerModule } from 'primeng/divider';
import { TooltipModule } from 'primeng/tooltip';
import { MessageModule } from 'primeng/message';
import { MessagesModule } from 'primeng/messages';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { DynamicDialogModule } from 'primeng/dynamicdialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { TabViewModule } from 'primeng/tabview';
import { ScrollerModule } from 'primeng/scroller';
import { DialogService } from 'primeng/dynamicdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

// Components
import { FloorPlanEditorComponent } from './floor-plan-editor.component';
import { FloorPlanViewComponent } from './floor-plan-view.component';
import { FloorPlanListComponent } from './floor-plan-list.component';
import { BoothBookingDialogComponent } from './booth-booking-dialog.component';

// ABP Guards
import { AuthGuard, permissionGuard } from '@abp/ng.core';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    component: FloorPlanListComponent,
    data: { title: 'Lista Planów Sali' }
  },
  {
    path: 'view',
    component: FloorPlanViewComponent,
    data: { title: 'Plan Sali' }
  },
  {
    path: 'view/:id',
    component: FloorPlanViewComponent,
    data: { title: 'Podgląd Planu Sali' }
  },
  {
    path: 'editor',
    component: FloorPlanEditorComponent,
    canActivate: [AuthGuard],
    data: {
      title: 'Edytor Planu Sali',
      requiredPermissions: ['FloorPlans.Create', 'FloorPlans.Design']
    }
  },
  {
    path: 'editor/:id',
    component: FloorPlanEditorComponent,
    canActivate: [AuthGuard],
    data: {
      title: 'Edytuj Plan Sali',
      requiredPermissions: ['FloorPlans.Edit', 'FloorPlans.Design']
    }
  }
];

@NgModule({
  declarations: [
    FloorPlanEditorComponent,
    FloorPlanViewComponent,
    FloorPlanListComponent,
    BoothBookingDialogComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes),

    // PrimeNG
    ButtonModule,
    InputTextModule,
    TextareaModule,
    InputNumberModule,
    CalendarModule,
    CheckboxModule,
    SelectButtonModule,
    DropdownModule,
    BadgeModule,
    DividerModule,
    TooltipModule,
    MessageModule,
    MessagesModule,
    ProgressSpinnerModule,
    DynamicDialogModule,
    ConfirmDialogModule,
    ToastModule,
    TabViewModule,
    ScrollerModule
  ],
  providers: [
    DialogService,
    MessageService,
    ConfirmationService
  ]
})
export class FloorPlanModule { }