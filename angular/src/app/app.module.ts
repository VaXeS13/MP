import { CoreModule, provideAbpCore, withOptions } from '@abp/ng.core';
import { provideAbpOAuth } from '@abp/ng.oauth';
import { provideSettingManagementConfig } from '@abp/ng.setting-management/config';
import { provideFeatureManagementConfig } from '@abp/ng.feature-management';
import { ThemeSharedModule, provideAbpThemeShared,} from '@abp/ng.theme.shared';
import { provideIdentityConfig } from '@abp/ng.identity/config';
import { provideAccountConfig } from '@abp/ng.account/config';
import { provideTenantManagementConfig } from '@abp/ng.tenant-management/config';
import { registerLocale } from '@abp/ng.core/locale';
import { NgModule, LOCALE_ID  } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { environment } from '../environments/environment';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { SharedModule } from './shared/shared.module';
import { APP_ROUTE_PROVIDER } from './route.provider';

import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { CalendarModule } from 'primeng/calendar';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { CardModule } from 'primeng/card';
import { ToolbarModule } from 'primeng/toolbar';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { CommonModule } from '@angular/common';
import { providePrimeNG } from 'primeng/config';

import { registerLocaleData } from '@angular/common';
import localePl from '@angular/common/locales/pl';
import localeDe from '@angular/common/locales/de';
import localeTr from '@angular/common/locales/tr';
import localeFr from '@angular/common/locales/fr';
import localeEs from '@angular/common/locales/es';
import localeIt from '@angular/common/locales/it';
import localeCs from '@angular/common/locales/cs';
import Aura from '@primeng/themes/aura';


import { SubdomainHeaderInterceptor } from './interceptors/subdomain-header.interceptor';
import { SubdomainTestComponent } from './subdomain-test/subdomain-test/subdomain-test.component';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { TenantInterceptor } from './interceptors/tenant.interceptor';
import { TenantService } from './services/tenant.service';
import { AuthCallbackComponent } from './auth-callback/auth-callback.component';
import { NavbarLayoutComponent } from './shared/layouts/navbar-layout/navbar-layout.component';
import { VALIDATION_ERROR_TEMPLATE, VALIDATION_INVALID_CLASSES, VALIDATION_TARGET_SELECTOR, NgxValidateCoreModule } from '@ngx-validate/core';
import { ValidationErrorComponent } from './shared/components/validation-error/validation-error.component';
import { CartModule } from './cart/cart.module';
import { ChatNotificationComponent } from './shared/components/chat-notification/chat-notification.component';

// Rejestracja wszystkich obsługiwanych lokalizacji
registerLocaleData(localePl, 'pl-PL'); // Polski (pełny kod)
registerLocaleData(localePl, 'pl'); // Polski (skrócony kod dla DatePipe)
registerLocaleData(localeDe, 'de-DE'); // Niemiecki (pełny kod)
registerLocaleData(localeDe, 'de'); // Niemiecki (skrócony kod dla DatePipe)
registerLocaleData(localeTr, 'tr'); // Turecki
registerLocaleData(localeFr, 'fr'); // Francuski
registerLocaleData(localeEs, 'es'); // Hiszpański
registerLocaleData(localeIt, 'it'); // Włoski
registerLocaleData(localeCs, 'cs'); // Czeski
@NgModule({
  declarations: [AppComponent,
    SubdomainTestComponent,
    AuthCallbackComponent,
    NavbarLayoutComponent,
    ValidationErrorComponent],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    FormsModule,
    ReactiveFormsModule,
    AppRoutingModule,
    CoreModule,
    ThemeSharedModule,
    SharedModule,
    CartModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    DropdownModule,
    CalendarModule,
    InputNumberModule,
    TextareaModule,
    TagModule,
    CardModule,
    ToolbarModule,
    ConfirmDialogModule,
    ToastModule,
    TooltipModule,
    CommonModule,
    NgxValidateCoreModule,
    ChatNotificationComponent
  ],
  providers: [
    APP_ROUTE_PROVIDER,
    provideAbpCore(
      withOptions({
        environment,
        registerLocaleFn: registerLocale(),
      }),
    ),
    provideAbpOAuth(),
    provideIdentityConfig(),
    provideSettingManagementConfig(),
    provideFeatureManagementConfig(),
    provideAccountConfig(),
    provideTenantManagementConfig(),
    provideAbpThemeShared(),
    MessageService,
    ConfirmationService, 
        providePrimeNG({
            theme: {
                preset: Aura,
                options: {
                    prefix: 'p',
                    darkModeSelector: 'system',
                    cssLayer: true,
                    ripple: true
                }
            }
        }),
   TenantService,
    {
      provide: HTTP_INTERCEPTORS,
      useClass: TenantInterceptor,
      multi: true,
    },
    {
      provide: VALIDATION_ERROR_TEMPLATE,
      useValue: ValidationErrorComponent,
    },
    {
      provide: VALIDATION_TARGET_SELECTOR,
      useValue: '.form-group',
    },
    {
      provide: VALIDATION_INVALID_CLASSES,
      useValue: 'is-invalid',
    },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
