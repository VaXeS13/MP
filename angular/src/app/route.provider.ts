import { RoutesService } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];

function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
      {
        path: '/',
        name: '::Menu:Home',
        iconClass: 'fas fa-home',
        order: 1,
      },
      {
        path: '/booths',
        name: '::Menu:BoothsManagement',
        iconClass: 'fas fa-cogs',
        order: 2,
        requiredPolicy: 'MP.Booths.Management',
      },
      {
        path: '/booths',
        name: '::Menu:BoothsList',
        parentName: '::Menu:BoothsManagement',
        iconClass: 'fas fa-list',
        order: 1,
        requiredPolicy: 'MP.Booths',
      },
      {
        path: '/booths/settings',
        name: '::Menu:MinimumGapSettings',
        parentName: '::Menu:BoothsManagement',
        iconClass: 'fas fa-calendar-day',
        order: 2,
        requiredPolicy: 'MP.Booths.ManageSettings',
      },
      {
        path: '/my-booths',
        name: '::Menu:MyBooths',
        iconClass: 'fas fa-store',
        order: 3,
        requiredPolicy: 'MP.Booths',
      },
      {
        path: '/my-booths/items',
        name: '::Menu:MyItems',
        parentName: '::Menu:MyBooths',
        iconClass: 'fas fa-box',
        order: 1,
        requiredPolicy: 'MP.Rentals',
      },
      {
        path: '/items',
        name: '::Menu:Items',
        iconClass: 'fas fa-shopping-bag',
        order: 3.5,
      },
      {
        path: '/items/list',
        name: '::Menu:MyItemsList',
        parentName: '::Menu:Items',
        iconClass: 'fas fa-list',
        order: 1,
      },
      {
        path: '/items/sheets',
        name: '::Menu:ItemSheets',
        parentName: '::Menu:Items',
        iconClass: 'fas fa-file-alt',
        order: 2,
      },
      {
        path: '/dashboard',
        name: '::Menu:Dashboard',
        iconClass: 'fas fa-chart-line',
        order: 4,
        requiredPolicy: 'MP.Dashboard',
      },
      {
        path: '/customer-dashboard',
        name: '::Menu:CustomerDashboard',
        iconClass: 'fas fa-user-chart',
        order: 5,
        requiredPolicy: 'MP.CustomerDashboard.ViewDashboard',
      },
      {
        path: '/administration',
        name: '::Menu:Administration',
        iconClass: 'fas fa-cog',
        order: 100,
        requiredPolicy: 'AbpIdentity.Users',
      },
      {
        path: '/identity/users',
        name: '::Menu:Users',
        parentName: '::Menu:Administration',
        iconClass: 'fas fa-users',
        order: 1,
        requiredPolicy: 'AbpIdentity.Users',
      },
      {
        path: '/identity/roles',
        name: '::Menu:Roles',
        parentName: '::Menu:Administration',
        iconClass: 'fas fa-user-tag',
        order: 2,
        requiredPolicy: 'AbpIdentity.Roles',
      },
      {
        path: '/tenant-management/tenants',
        name: '::Menu:Tenants',
        parentName: '::Menu:Administration',
        iconClass: 'fas fa-building',
        order: 3,
        requiredPolicy: 'AbpTenantManagement.Tenants',
      },
      {
        path: '/booth-types',
        name: '::Menu:BoothTypes',
        parentName: '::Menu:Administration',
        iconClass: 'fas fa-tags',
        order: 4,
      },
      {
        path: '/payment-providers',
        name: '::Menu:PaymentProviders',
        parentName: '::Menu:Administration',
        iconClass: 'fas fa-credit-card',
        order: 5,
        requiredPolicy: 'MP.PaymentProviders.Manage',
      },
      {
        path: '/terminal-settings',
        name: 'Terminal Payment Providers',
        parentName: '::Menu:Administration',
        iconClass: 'fas fa-cash-register',
        order: 6,
      },
      {
        path: '/seller-checkout',
        name: 'Seller Checkout',
        iconClass: 'fas fa-shopping-cart',
        order: 6,
      },
      {
        path: '/floor-plans',
        name: '::Menu:FloorPlans',
        iconClass: 'fas fa-map',
        order: 7,
      },
      {
        path: '/floor-plans/list',
        name: '::Menu:FloorPlanList',
        parentName: '::Menu:FloorPlans',
        iconClass: 'fas fa-list',
        order: 1,
      },
      {
        path: '/floor-plans/view',
        name: '::Menu:FloorPlanView',
        parentName: '::Menu:FloorPlans',
        iconClass: 'fas fa-eye',
        order: 2,
      },
      {
        path: '/floor-plans/editor',
        name: '::Menu:FloorPlanEditor',
        parentName: '::Menu:FloorPlans',
        iconClass: 'fas fa-edit',
        order: 3,
        requiredPolicy: 'MP.Booths.Create',
      },
  ]);
}
