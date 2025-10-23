import { Injectable, inject } from '@angular/core';
import { SignalRService } from './signalr.service';
import { Subject, Observable } from 'rxjs';

export interface ItemSoldUpdate {
  itemId: string;
  itemName: string;
  salePrice: number;
  soldAt: Date;
  rentalId?: string;
}

export interface RentalSalesUpdate {
  rentalId: string;
  totalItemsSold: number;
  totalSalesAmount: number;
  lastSaleTime: Date;
}

@Injectable({
  providedIn: 'root'
})
export class SalesSignalRService {
  private readonly signalRService = inject(SignalRService);

  private itemSoldSubject$ = new Subject<ItemSoldUpdate>();
  private rentalSalesUpdatedSubject$ = new Subject<RentalSalesUpdate>();

  /**
   * Observable for item sold events
   * Emits when an item is sold in real-time
   */
  get itemSold(): Observable<ItemSoldUpdate> {
    return this.itemSoldSubject$.asObservable();
  }

  /**
   * Observable for rental sales updates
   * Emits when sales statistics for a rental are updated
   */
  get rentalSalesUpdated(): Observable<RentalSalesUpdate> {
    return this.rentalSalesUpdatedSubject$.asObservable();
  }

  /**
   * Initialize sales SignalR listeners
   * Call this after SignalR connections are established
   */
  initialize(): void {
    const salesHub = this.signalRService.salesHub;

    if (!salesHub) {
      console.error('SalesSignalRService: SalesHub not available - cannot initialize');
      return;
    }

    console.log('SalesSignalRService: Initializing sales listeners...');
    console.log('SalesSignalRService: Hub connection state:', salesHub.state);

    // Remove any existing listeners to avoid duplicates
    salesHub.off('ItemSold');
    salesHub.off('RentalSalesUpdated');

    // Listen for item sold events
    salesHub.on('ItemSold', (update: any) => {
      console.log('SalesSignalRService: ✅ Received ItemSold event:', update);

      const itemSoldUpdate: ItemSoldUpdate = {
        itemId: update.itemId,
        itemName: update.itemName,
        salePrice: update.salePrice,
        soldAt: update.soldAt ? new Date(update.soldAt) : new Date(),
        rentalId: update.rentalId
      };

      console.log('SalesSignalRService: Broadcasting ItemSold to subscribers:', itemSoldUpdate);
      this.itemSoldSubject$.next(itemSoldUpdate);
    });

    // Listen for rental sales updates (if backend sends them)
    salesHub.on('RentalSalesUpdated', (update: any) => {
      console.log('SalesSignalRService: ✅ Received RentalSalesUpdated event:', update);

      const rentalUpdate: RentalSalesUpdate = {
        rentalId: update.rentalId,
        totalItemsSold: update.totalItemsSold,
        totalSalesAmount: update.totalSalesAmount,
        lastSaleTime: update.lastSaleTime ? new Date(update.lastSaleTime) : new Date()
      };

      console.log('SalesSignalRService: Broadcasting RentalSalesUpdated to subscribers:', rentalUpdate);
      this.rentalSalesUpdatedSubject$.next(rentalUpdate);
    });

    console.log('SalesSignalRService: ✅ Initialized and listening for sales updates');
  }

  /**
   * Subscribe to sales updates for a specific user's rental
   */
  async subscribeToRentalSales(rentalId: string): Promise<void> {
    const salesHub = this.signalRService.salesHub;

    if (!salesHub) {
      console.error('SalesSignalRService: SalesHub not available');
      return;
    }

    try {
      console.log(`SalesSignalRService: Subscribing to rental sales: ${rentalId}`);
      await salesHub.invoke('SubscribeToRentalSales', rentalId);
      console.log(`SalesSignalRService: ✅ Subscribed to rental sales: ${rentalId}`);
    } catch (error) {
      console.error('SalesSignalRService: Failed to subscribe to rental sales', error);
    }
  }

  /**
   * Unsubscribe from sales updates for a specific rental
   */
  async unsubscribeFromRentalSales(rentalId: string): Promise<void> {
    const salesHub = this.signalRService.salesHub;

    if (!salesHub) {
      console.error('SalesSignalRService: SalesHub not available');
      return;
    }

    try {
      console.log(`SalesSignalRService: Unsubscribing from rental sales: ${rentalId}`);
      await salesHub.invoke('UnsubscribeFromRentalSales', rentalId);
      console.log(`SalesSignalRService: ✅ Unsubscribed from rental sales: ${rentalId}`);
    } catch (error) {
      console.error('SalesSignalRService: Failed to unsubscribe from rental sales', error);
    }
  }
}
