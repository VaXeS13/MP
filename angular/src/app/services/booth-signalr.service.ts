import { Injectable, inject } from '@angular/core';
import { SignalRService } from './signalr.service';
import { Subject } from 'rxjs';

export interface BoothStatusUpdate {
  boothId: string;
  status: string;
  isOccupied: boolean;
  currentRentalId?: string;
  occupiedUntil?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class BoothSignalRService {
  private readonly signalRService = inject(SignalRService);

  private boothStatusUpdated$ = new Subject<BoothStatusUpdate>();

  get boothUpdates() {
    return this.boothStatusUpdated$.asObservable();
  }

  /**
   * Initialize booth SignalR listeners
   */
  initialize(): void {
    const boothHub = this.signalRService.boothHub;

    if (!boothHub) {
      console.error('BoothSignalRService: BoothHub not available - cannot initialize');
      return;
    }

    console.log('BoothSignalRService: Initializing booth listeners...');
    console.log('BoothSignalRService: Hub connection state:', boothHub.state);

    // Remove any existing listeners to avoid duplicates
    boothHub.off('BoothStatusUpdated');

    // Listen for booth status updates
    boothHub.on('BoothStatusUpdated', (update: any) => {
      console.log('BoothSignalRService: ✅ Received BoothStatusUpdated event:', update);

      const boothUpdate: BoothStatusUpdate = {
        boothId: update.boothId,
        status: update.status,
        isOccupied: update.isOccupied,
        currentRentalId: update.currentRentalId,
        occupiedUntil: update.occupiedUntil ? new Date(update.occupiedUntil) : undefined
      };

      console.log('BoothSignalRService: Broadcasting update to subscribers:', boothUpdate);
      this.boothStatusUpdated$.next(boothUpdate);
    });

    console.log('BoothSignalRService: ✅ Initialized and listening for booth updates');
  }

  /**
   * Subscribe to specific floor plan updates
   */
  async subscribeToFloorPlan(floorPlanId: string): Promise<void> {
    const boothHub = this.signalRService.boothHub;

    if (!boothHub) {
      return;
    }

    try {
      await boothHub.invoke('SubscribeToFloorPlan', floorPlanId);
      console.log(`Subscribed to floor plan: ${floorPlanId}`);
    } catch (error) {
      console.error('Failed to subscribe to floor plan', error);
    }
  }

  /**
   * Unsubscribe from floor plan updates
   */
  async unsubscribeFromFloorPlan(floorPlanId: string): Promise<void> {
    const boothHub = this.signalRService.boothHub;

    if (!boothHub) {
      return;
    }

    try {
      await boothHub.invoke('UnsubscribeFromFloorPlan', floorPlanId);
      console.log(`Unsubscribed from floor plan: ${floorPlanId}`);
    } catch (error) {
      console.error('Failed to unsubscribe from floor plan', error);
    }
  }
}
