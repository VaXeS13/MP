import { Injectable, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { MessageService } from 'primeng/api';
import { SignalRService } from './signalr.service';

export interface NotificationMessage {
  id: string;
  type: string;
  title: string;
  message: string;
  severity: 'success' | 'info' | 'warn' | 'error';
  actionUrl?: string;
  createdAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly signalRService = inject(SignalRService);
  private readonly messageService = inject(MessageService);

  private notificationReceived$ = new Subject<NotificationMessage>();
  private unreadCount = 0;

  get notifications(): Subject<NotificationMessage> {
    return this.notificationReceived$;
  }

  get unreadNotificationCount(): number {
    return this.unreadCount;
  }

  /**
   * Initialize notification listeners
   * Call this after SignalR connections are established
   */
  initialize(): void {
    const notificationHub = this.signalRService.notificationHub;

    if (!notificationHub) {
      console.warn('NotificationService: NotificationHub not available');
      return;
    }

    // Listen for incoming notifications
    notificationHub.on('ReceiveNotification', (notification: any) => {
      this.handleNotification(notification);
    });

    console.log('NotificationService: Initialized and listening for notifications');
  }

  /**
   * Handle incoming notification
   */
  private handleNotification(notification: any): void {
    const message: NotificationMessage = {
      id: notification.id,
      type: notification.type,
      title: notification.title,
      message: notification.message,
      severity: this.mapSeverity(notification.severity),
      actionUrl: notification.actionUrl,
      createdAt: new Date(notification.createdAt)
    };

    // Emit notification to subscribers
    this.notificationReceived$.next(message);

    // Increment unread count
    this.unreadCount++;

    // Show toast notification using PrimeNG
    this.showToast(message);
  }

  /**
   * Show toast notification
   */
  private showToast(notification: NotificationMessage): void {
    this.messageService.add({
      severity: notification.severity,
      summary: notification.title,
      detail: notification.message,
      life: 5000,
      sticky: notification.severity === 'error',
      data: notification
    });
  }

  /**
   * Map severity string to PrimeNG severity
   */
  private mapSeverity(severity: string): 'success' | 'info' | 'warn' | 'error' {
    switch (severity?.toLowerCase()) {
      case 'success':
        return 'success';
      case 'warning':
        return 'warn';
      case 'error':
        return 'error';
      default:
        return 'info';
    }
  }

  /**
   * Mark notification as read
   */
  async markAsRead(notificationId: string): Promise<void> {
    const notificationHub = this.signalRService.notificationHub;

    if (!notificationHub) {
      return;
    }

    try {
      await notificationHub.invoke('MarkAsRead', notificationId);
      this.unreadCount = Math.max(0, this.unreadCount - 1);
    } catch (error) {
      console.error('Failed to mark notification as read', error);
    }
  }

  /**
   * Clear unread count
   */
  clearUnreadCount(): void {
    this.unreadCount = 0;
  }
}
