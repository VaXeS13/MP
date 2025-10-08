import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { BoothTypeListComponent } from './booth-type-list/booth-type-list.component';

const routes: Routes = [
  {
    path: '',
    component: BoothTypeListComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class BoothTypeRoutingModule { }