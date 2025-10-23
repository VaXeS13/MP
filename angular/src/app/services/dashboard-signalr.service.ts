import { Injectable, inject } from '@angular/core';
import { SignalRService } from './signalr.service';
import { Subject, Observable } from 'rxjs';

export interface DashboardDataUpdate {
  overview?: any;
  sales?: any;
  booth?: any;
  financial?: any;
  payment?: any;
  updatedAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardSignalRService {
  private readonly signalRService = inject(SignalRService);

  private dashboardDataUpdatedSubject$ = new Subject<DashboardDataUpdate>();
  private dashboardRefreshNeededSubject$ = new Subject<void>();

  /**
   * Observable for dashboard data updates
   * Emits when dashboard data changes in real-time with actual data
   */
  get dashboardDataUpdated(): Observable<DashboardDataUpdate> {
    return this.dashboardDataUpdatedSubject$.asObservable();
  }

  /**
   * Observable for dashboard refresh triggers
   * Emits when data needs to be refreshed from server
   * Used when partial updates aren't sufficient
   */
  get dashboardRefreshNeeded(): Observable<void> {
    return this.dashboardRefreshNeededSubject$.asObservable();
  }

  /**
   * Initialize dashboard SignalR listeners
   * Call this after SignalR connections are established
   */
  initialize(): void {
    const dashboardHub = this.signalRService.dashboardHub;

    if (!dashboardHub) {
      console.error('DashboardSignalRService: DashboardHub not available - cannot initialize');
      return;
    }

    console.log('DashboardSignalRService: Initializing dashboard listeners...');
    console.log('DashboardSignalRService: Hub connection state:', dashboardHub.state);

    // Remove any existing listeners to avoid duplicates
    dashboardHub.off('DashboardUpdated');
    dashboardHub.off('DashboardRefreshNeeded');

    // Listen for dashboard data updates with actual data
    dashboardHub.on('DashboardUpdated', (data: any) => {
      console.log('DashboardSignalRService: ✅ Received DashboardUpdated event with data:', data);

      const update: DashboardDataUpdate = {
        overview: data.overview,
        sales: data.sales,
        booth: data.booth,
        financial: data.financial,
        payment: data.payment,
        updatedAt: new Date()
      };

      console.log('DashboardSignalRService: Broadcasting dashboard update to subscribers:', update);
      this.dashboardDataUpdatedSubject$.next(update);
    });

    // Listen for full refresh triggers (when only a refresh is needed)
    dashboardHub.on('DashboardRefreshNeeded', () => {
      console.log('DashboardSignalRService: ✅ Received DashboardRefreshNeeded trigger');
      console.log('DashboardSignalRService: Broadcasting dashboard refresh trigger to subscribers');
      this.dashboardRefreshNeededSubject$.next();
    });

    console.log('DashboardSignalRService: ✅ Initialized and listening for dashboard updates');
  }

  /**
   * Request immediate dashboard refresh from client side
   */
  async requestDashboardUpdate(): Promise<void> {
    const dashboardHub = this.signalRService.dashboardHub;

    if (!dashboardHub) {
      console.error('DashboardSignalRService: DashboardHub not available');
      return;
    }

    try {
      console.log('DashboardSignalRService: Requesting dashboard update from server');
      await dashboardHub.invoke('RequestDashboardUpdate');
      console.log('DashboardSignalRService: ✅ Dashboard update requested');
    } catch (error) {
      console.error('DashboardSignalRService: Failed to request dashboard update', error);
    }
  }
}
