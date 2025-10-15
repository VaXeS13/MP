import { AuthGuard, authGuard, permissionGuard } from '@abp/ng.core';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
//import { BoothListComponent } from './booth/booth-list/booth-list.component';

const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadChildren: () => import('./home/home.module').then(m => m.HomeModule),
  },
  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(m => m.AccountModule.forLazy()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(m => m.IdentityModule.forLazy()),
  },
  {
    path: 'tenant-management',
    loadChildren: () =>
      import('@abp/ng.tenant-management').then(m => m.TenantManagementModule.forLazy()),
  },
  {
    path: 'setting-management',
    loadChildren: () =>
      import('@abp/ng.setting-management').then(m => m.SettingManagementModule.forLazy()),
  },
  {
    path: 'payment-providers',
    loadChildren: () => import('./payment-providers/payment-providers.module').then(m => m.PaymentProvidersModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.PaymentProviders.Manage'
    }
  },
  {
    path: 'seller-checkout',
    loadComponent: () => import('./seller-checkout/seller-checkout.component').then(m => m.SellerCheckoutComponent),
    canActivate: [authGuard],
  },
  {
    path: 'terminal-settings',
    loadComponent: () => import('./terminal-settings/terminal-settings.component').then(m => m.TerminalSettingsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'tenant-currency-settings',
    loadComponent: () => import('./tenant-currency-settings/tenant-currency-settings.component').then(m => m.TenantCurrencySettingsComponent),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Tenant.ManageCurrency'
    }
  },
  // NASZE ROUTES
  {
    path: 'booths',
    loadChildren: () => import('./booth/booth/booth.module').then(m => m.BoothModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Booths'
    }
  },
  {
    path: 'my-booths',
    loadChildren: () => import('./booth/my-booths/my-booths.module').then(m => m.MyBoothsModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Booths'
    }
  },
  {
    path: 'promotions',
    loadChildren: () => import('./promotions/promotions.module').then(m => m.PromotionsModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Promotions'
    }
  },
  {
    path: 'homepage-content',
    loadChildren: () => import('./homepage-content/homepage-content.module').then(m => m.HomepageContentModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.HomePageContent'
    }
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Dashboard'
    }
  },
  {
    path: 'rentals',
    loadChildren: () => import('./rental/rental.module').then(m => m.RentalModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.Rentals'
    }
  },
  {
    path: 'rentals/payment-success/:transactionId',
    loadComponent: () => import('./payment-success/payment-success.component').then(m => m.PaymentSuccessComponent)
  },
  {
    path: 'booth-types',
    loadChildren: () => import('./booth-type/booth-type.module').then(m => m.BoothTypeModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'floor-plans',
    loadChildren: () => import('./floor-plan/floor-plan.module').then(m => m.FloorPlanModule),
    data: {
      title: 'Plany Sali'
    }
  },
  {
    path: 'cart',
    loadChildren: () => import('./cart/cart.module').then(m => m.CartModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'customer-dashboard',
    loadChildren: () => import('./customer-dashboard/customer-dashboard.module').then(m => m.CustomerDashboardModule),
    canActivate: [AuthGuard, permissionGuard],
    data: {
      requiredPolicy: 'MP.CustomerDashboard.ViewDashboard'
    }
  },
  {
    path: 'chat',
    loadComponent: () => import('./shared/components/chat/chat.component').then(m => m.ChatComponent),
    canActivate: [authGuard]
  },
  {
    path: 'items',
    loadChildren: () => import('./items/items.module').then(m => m.ItemsModule),
    canActivate: [authGuard]
  },
  {
    path: 'profile',
    loadComponent: () => import('./profile/profile.component').then(m => m.ProfileComponent),
    canActivate: [authGuard]
  },
  /*{
    path: 'booths',
    component: BoothListComponent,
    canActivate: [authGuard]

  }*/
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {})],
  exports: [RouterModule],
})
export class AppRoutingModule {}
