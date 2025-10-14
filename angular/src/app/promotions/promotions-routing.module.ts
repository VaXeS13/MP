import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { PromotionListComponent } from './promotion-list/promotion-list.component';
import { PromotionFormComponent } from './promotion-form/promotion-form.component';
import { authGuard, permissionGuard } from '@abp/ng.core';

const routes: Routes = [
  {
    path: '',
    component: PromotionListComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Promotions'
    }
  },
  {
    path: 'new',
    component: PromotionFormComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Promotions.Create'
    }
  },
  {
    path: ':id/edit',
    component: PromotionFormComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Promotions.Edit'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PromotionsRoutingModule { }
