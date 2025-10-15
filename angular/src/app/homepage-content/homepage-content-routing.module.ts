import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomepageSectionListComponent } from './homepage-section-list/homepage-section-list.component';
import { HomepageSectionFormComponent } from './homepage-section-form/homepage-section-form.component';
import { authGuard, permissionGuard } from '@abp/ng.core';

const routes: Routes = [
  {
    path: '',
    component: HomepageSectionListComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.HomePageContent'
    }
  },
  {
    path: 'new',
    component: HomepageSectionFormComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.HomePageContent.Create'
    }
  },
  {
    path: ':id/edit',
    component: HomepageSectionFormComponent,
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.HomePageContent.Edit'
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class HomepageContentRoutingModule { }
