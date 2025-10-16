import { CoreModule } from '@abp/ng.core';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { NgModule } from '@angular/core';
import { ThemeSharedModule } from '@abp/ng.theme.shared';
import { NgxValidateCoreModule } from '@ngx-validate/core';
import { NotificationBellComponent } from './components/notification-bell/notification-bell.component';
import { NotificationCenterComponent } from './components/notification-center/notification-center.component';
import { ChatIconComponent } from './components/chat-icon/chat-icon.component';
import { PromotionNotificationWidgetComponent } from './components/promotion-notification-widget/promotion-notification-widget.component';
import { ButtonModule } from 'primeng/button';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { TooltipModule } from 'primeng/tooltip';
import { BadgeModule } from 'primeng/badge';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    NotificationBellComponent,
    ChatIconComponent,
    PromotionNotificationWidgetComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    CoreModule,
    ThemeSharedModule,
    NgbDropdownModule,
    NgxValidateCoreModule,
    ButtonModule,
    OverlayPanelModule,
    TooltipModule,
    BadgeModule,
    NotificationCenterComponent
  ],
  exports: [
    CommonModule,
    FormsModule,
    CoreModule,
    ThemeSharedModule,
    NgbDropdownModule,
    NgxValidateCoreModule,
    NotificationBellComponent,
    ChatIconComponent,
    PromotionNotificationWidgetComponent
  ],
  providers: []
})
export class SharedModule {}
