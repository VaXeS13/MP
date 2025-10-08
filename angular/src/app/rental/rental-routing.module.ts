import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PermissionGuard } from '@abp/ng.core';
import { RentalCalendarComponent } from './rental-calendar/rental-calendar.component';
import { RentalListComponent } from './rental-list/rental-list.component';
import { RentalBoothSelectionComponent } from './rental-booth-selection/rental-booth-selection.component';
import { RentalDetailsComponent } from './rental-details/rental-details.component';

const routes: Routes = [
  {
    path: '',
    component: RentalBoothSelectionComponent,
    canActivate: [PermissionGuard],
    data: {
      requiredPolicy: 'MP.Rentals',
    },
  },
  {
    path: 'my-rentals',
    component: RentalListComponent,
    canActivate: [PermissionGuard],
    data: {
      requiredPolicy: 'MP.Rentals',
    },
  },
  {
    path: 'book/:boothId',
    component: RentalCalendarComponent,
    canActivate: [PermissionGuard],
    data: {
      requiredPolicy: 'MP.Rentals',
    },
  },
  {
    path: ':id',
    component: RentalDetailsComponent,
    canActivate: [PermissionGuard],
    data: {
      requiredPolicy: 'MP.Rentals',
    },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class RentalRoutingModule {}