import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerDashboardComponent } from './customer-dashboard.component';
import { MyItemsComponent } from './my-items/my-items.component';
import { MyRentalsComponent } from './my-rentals/my-rentals.component';
import { SettlementsComponent } from './settlements/settlements.component';

const routes: Routes = [
  {
    path: '',
    component: CustomerDashboardComponent
  },
  {
    path: 'my-items',
    component: MyItemsComponent
  },
  {
    path: 'my-rentals',
    component: MyRentalsComponent
  },
  {
    path: 'settlements',
    component: SettlementsComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CustomerDashboardRoutingModule { }
