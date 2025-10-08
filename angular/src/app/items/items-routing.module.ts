import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    loadComponent: () => import('./item-list/item-list.component').then(m => m.ItemListComponent)
  },
  {
    path: 'sheets',
    loadComponent: () => import('./item-sheet-list/item-sheet-list.component').then(m => m.ItemSheetListComponent)
  },
  {
    path: 'sheets/:id',
    loadComponent: () => import('./item-sheet-detail/item-sheet-detail.component').then(m => m.ItemSheetDetailComponent)
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ItemsRoutingModule { }
