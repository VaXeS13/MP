import { Injectable, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { AuthService, ConfigStateService } from '@abp/ng.core';

export enum ConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting'
}

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private readonly authService = inject(AuthService);
  private readonly configState = inject(ConfigStateService);

  private notificationConnection?: signalR.HubConnection;
  private dashboardConnection?: signalR.HubConnection;
  private boothConnection?: signalR.HubConnection;
  private salesConnection?: signalR.HubConnection;
  private chatConnection?: signalR.HubConnection;

  private connectionState$ = new BehaviorSubject<ConnectionState>(ConnectionState.Disconnected);

  get connectionState(): Observable<ConnectionState> {
    return this.connectionState$.asObservable();
  }

  get notificationHub(): signalR.HubConnection | undefined {
    return this.notificationConnection;
  }

  get dashboardHub(): signalR.HubConnection | undefined {
    return this.dashboardConnection;
  }

  get boothHub(): signalR.HubConnection | undefined {
    return this.boothConnection;
  }

  get salesHub(): signalR.HubConnection | undefined {
    return this.salesConnection;
  }

  get chatHub(): signalR.HubConnection | undefined {
    return this.chatConnection;
  }

  /**
   * Start all SignalR connections
   */
  async startConnections(): Promise<void> {
    if (!this.authService.isAuthenticated) {
      console.log('SignalR: User not authenticated, skipping connection');
      return;
    }

    console.log('SignalR: Starting connections to all hubs...');
    this.connectionState$.next(ConnectionState.Connecting);

    const baseUrl = this.configState.getDeep('apis.default.url') || 'https://localhost:44377';
    console.log('SignalR: Base URL:', baseUrl);
    const accessToken = await this.getAccessToken();
    console.log('SignalR: Access token obtained:', accessToken ? 'Yes' : 'No');

    try {
      // Start all hubs in parallel
      console.log('SignalR: Connecting to hubs in parallel...');
      await Promise.all([
        this.startNotificationHub(baseUrl, accessToken),
        this.startDashboardHub(baseUrl, accessToken),
        this.startBoothHub(baseUrl, accessToken),
        this.startSalesHub(baseUrl, accessToken),
        this.startChatHub(baseUrl, accessToken)
      ]);

      this.connectionState$.next(ConnectionState.Connected);
      console.log('SignalR: ✅ All connections established successfully');
      console.log('SignalR: Booth Hub State:', this.boothConnection?.state);
      console.log('SignalR: Notification Hub State:', this.notificationConnection?.state);
    } catch (error) {
      console.error('SignalR: ❌ Failed to start connections', error);
      this.connectionState$.next(ConnectionState.Disconnected);
    }
  }

  /**
   * Stop all SignalR connections
   */
  async stopConnections(): Promise<void> {
    const promises: Promise<void>[] = [];

    if (this.notificationConnection) {
      promises.push(this.notificationConnection.stop());
    }
    if (this.dashboardConnection) {
      promises.push(this.dashboardConnection.stop());
    }
    if (this.boothConnection) {
      promises.push(this.boothConnection.stop());
    }
    if (this.salesConnection) {
      promises.push(this.salesConnection.stop());
    }
    if (this.chatConnection) {
      promises.push(this.chatConnection.stop());
    }

    await Promise.all(promises);
    this.connectionState$.next(ConnectionState.Disconnected);
    console.log('SignalR: All connections stopped');
  }

  private async startNotificationHub(baseUrl: string, accessToken: string): Promise<void> {
    this.notificationConnection = this.createConnection(`${baseUrl}/signalr-hubs/notifications`, accessToken);
    await this.notificationConnection.start();
  }

  private async startDashboardHub(baseUrl: string, accessToken: string): Promise<void> {
    this.dashboardConnection = this.createConnection(`${baseUrl}/signalr-hubs/dashboard`, accessToken);
    await this.dashboardConnection.start();
  }

  private async startBoothHub(baseUrl: string, accessToken: string): Promise<void> {
    const hubUrl = `${baseUrl}/signalr-hubs/booths`;
    console.log('SignalR: Starting BoothHub connection to:', hubUrl);
    this.boothConnection = this.createConnection(hubUrl, accessToken);
    await this.boothConnection.start();
    console.log('SignalR: ✅ BoothHub connected, state:', this.boothConnection.state);
  }

  private async startSalesHub(baseUrl: string, accessToken: string): Promise<void> {
    this.salesConnection = this.createConnection(`${baseUrl}/signalr-hubs/sales`, accessToken);
    await this.salesConnection.start();
  }

  private async startChatHub(baseUrl: string, accessToken: string): Promise<void> {
    this.chatConnection = this.createConnection(`${baseUrl}/signalr-hubs/chat`, accessToken);
    await this.chatConnection.start();
  }

  private createConnection(url: string, accessToken: string): signalR.HubConnection {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: () => accessToken,
        withCredentials: true
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          // Exponential backoff: 0s, 2s, 10s, 30s
          if (retryContext.previousRetryCount === 0) return 0;
          if (retryContext.previousRetryCount === 1) return 2000;
          if (retryContext.previousRetryCount === 2) return 10000;
          return 30000;
        }
      })
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.onreconnecting(() => {
      this.connectionState$.next(ConnectionState.Reconnecting);
      console.log(`SignalR: Reconnecting to ${url}`);
    });

    connection.onreconnected(() => {
      this.connectionState$.next(ConnectionState.Connected);
      console.log(`SignalR: Reconnected to ${url}`);
    });

    connection.onclose((error) => {
      this.connectionState$.next(ConnectionState.Disconnected);
      console.log(`SignalR: Connection closed for ${url}`, error);
    });

    return connection;
  }

  private async getAccessToken(): Promise<string> {
    // ABP provides the access token via AuthService
    const token = this.authService.getAccessToken();
    return token || '';
  }
}
