import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Routes } from '@angular/router';
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { CoreModule } from '@abp/ng.core';
import { MyBoothsComponent } from './my-booths.component';

const routes: Routes = [
  {
    path: '',
    component: MyBoothsComponent
  },
  {
    path: 'items',
    loadChildren: () => import('../../rental/rental.module').then(m => m.RentalModule)
  }
];

@NgModule({
  declarations: [
    MyBoothsComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    RouterModule.forChild(routes),
    NgbModule,
    CoreModule
  ]
})
export class MyBoothsModule { }