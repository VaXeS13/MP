import { Component, OnInit, OnDestroy } from '@angular/core';
import { TenantService } from './shared/services/tenant.service';
import { SignalRService } from './services/signalr.service';
import { NotificationService } from './services/notification.service';
import { BoothSignalRService } from './services/booth-signalr.service';
import { SalesSignalRService } from './services/sales-signalr.service';
import { DashboardSignalRService } from './services/dashboard-signalr.service';
import { ChatService } from './services/chat.service';
import { AuthService, ConfigStateService } from '@abp/ng.core';

@Component({
  standalone: false,
  selector: 'app-root',
  templateUrl: './app.component.html',
})
export class AppComponent implements OnInit, OnDestroy {

  constructor(
    public tenantService: TenantService,
    private signalRService: SignalRService,
    private notificationService: NotificationService,
    private boothSignalRService: BoothSignalRService,
    private salesSignalRService: SalesSignalRService,
    private dashboardSignalRService: DashboardSignalRService,
    private chatService: ChatService,
    private authService: AuthService,
    private configState: ConfigStateService
  ) {}

  ngOnInit(): void {
    // Tenant jest automatycznie rozpoznawany w TenantService constructor
    console.log('Current tenant:', this.tenantService.getCurrentTenant());

    // Initialize SignalR when user is authenticated
    if (this.authService.isAuthenticated) {
      console.log('AppComponent: User authenticated, initializing SignalR...');
      this.initializeSignalR();
    } else {
      console.log('AppComponent: User not authenticated, skipping SignalR initialization');
    }
  }

  async ngOnDestroy(): Promise<void> {
    await this.signalRService.stopConnections();
  }

  private async initializeSignalR(): Promise<void> {
    try {
      console.log('AppComponent: Starting SignalR connections...');
      await this.signalRService.startConnections();
      console.log('AppComponent: SignalR connections established');

      const currentUserId = this.configState.getOne('currentUser')?.id;
      if (!currentUserId) {
        console.error('AppComponent: Current user ID not found - cannot initialize SignalR services');
        return;
      }

      console.log('AppComponent: Initializing SignalR services with userId:', currentUserId);
      this.notificationService.initialize();
      this.boothSignalRService.initialize();
      this.salesSignalRService.initialize();
      this.dashboardSignalRService.initialize();
      this.chatService.initialize(currentUserId);
      console.log('AppComponent: SignalR initialized successfully - all hubs ready');
    } catch (error) {
      console.error('AppComponent: Failed to initialize SignalR', error);
    }
  }
}