import { Injectable, inject } from '@angular/core';
import { Subject, BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessageService } from 'primeng/api';
import { SignalRService } from './signalr.service';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface NotificationMessage {
  id: string;
  type: string;
  title: string;
  message: string;
  severity: 'success' | 'info' | 'warn' | 'error';
  actionUrl?: string;
  createdAt: Date;
}

export interface NotificationDto {
  id: string;
  type: string;
  title: string;
  message: string;
  severity: 'info' | 'success' | 'warning' | 'error';
  isRead: boolean;
  readAt?: string;
  actionUrl?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  creationTime: string;
  expiresAt?: string;
}

export interface NotificationListDto {
  items: NotificationDto[];
  totalCount: number;
  unreadCount: number;
}

export interface GetNotificationsInput {
  isRead?: boolean;
  severity?: 'info' | 'success' | 'warning' | 'error';
  type?: string;
  startDate?: string;
  endDate?: string;
  includeExpired?: boolean;
  skipCount?: number;
  maxResultCount?: number;
  sorting?: string;
}

export interface NotificationStatsDto {
  totalCount: number;
  unreadCount: number;
  readCount: number;
  expiredCount: number;
}

// Default empty stats object
export const DEFAULT_STATS: NotificationStatsDto = {
  totalCount: 0,
  unreadCount: 0,
  readCount: 0,
  expiredCount: 0
};

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly signalRService = inject(SignalRService);
  private readonly messageService = inject(MessageService);
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apis.default.url;

  private notificationReceived$ = new Subject<NotificationMessage>();
  private unreadCount$ = new BehaviorSubject<number>(0);
  private notificationList$ = new BehaviorSubject<NotificationListDto | null>(null);

  get notifications(): Subject<NotificationMessage> {
    return this.notificationReceived$;
  }

  get unreadNotificationCount(): Observable<number> {
    return this.unreadCount$.asObservable();
  }

  get notificationList(): Observable<NotificationListDto | null> {
    return this.notificationList$.asObservable();
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
    const currentCount = this.unreadCount$.value;
    this.unreadCount$.next(currentCount + 1);

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
   * Clear unread count
   */
  clearUnreadCount(): void {
    this.unreadCount$.next(0);
  }

  // API METHODS FOR BACKEND INTEGRATION

  /**
   * Get paginated notifications list
   */
  getNotifications(input: GetNotificationsInput = {}): Observable<NotificationListDto> {
    const params = this.buildHttpParams(input);
    return this.http.get<NotificationListDto>(`${this.baseUrl}/api/app/notification`, { params })
      .pipe(
        // Update the observable values with response
        map(response => {
          this.notificationList$.next(response);
          this.unreadCount$.next(response.unreadCount);
          return response;
        })
      );
  }

  /**
   * Get unread notifications
   */
  getUnreadNotifications(input: GetNotificationsInput = {}): Observable<NotificationListDto> {
    const params = this.buildHttpParams({ ...input, isRead: false });
    return this.http.get<NotificationListDto>(`${this.baseUrl}/api/app/notification/unread`, { params })
      .pipe(
        map(response => {
          this.notificationList$.next(response);
          this.unreadCount$.next(response.unreadCount);
          return response;
        })
      );
  }

  /**
   * Get all notifications (read + unread)
   */
  getAllNotifications(input: GetNotificationsInput = {}): Observable<NotificationListDto> {
    const params = this.buildHttpParams(input);
    return this.http.get<NotificationListDto>(`${this.baseUrl}/api/app/notification/all`, { params })
      .pipe(
        map(response => {
          this.notificationList$.next(response);
          this.unreadCount$.next(response.unreadCount);
          return response;
        })
      );
  }

  /**
   * Mark a notification as read
   */
  async markAsRead(notificationId: string): Promise<void> {
    const notificationHub = this.signalRService.notificationHub;

    if (!notificationHub) {
      return;
    }

    try {
      await notificationHub.invoke('MarkAsRead', notificationId);

      // Also call API to ensure consistency
      this.http.post(`${this.baseUrl}/api/app/notification/${notificationId}/mark-as-read`, {})
        .subscribe({
          next: () => {
            // Update local counts
            const currentCount = this.unreadCount$.value;
            this.unreadCount$.next(Math.max(0, currentCount - 1));
          },
          error: (error) => console.error('Failed to mark notification as read via API', error)
        });
    } catch (error) {
      console.error('Failed to mark notification as read', error);
    }
  }

  /**
   * Mark multiple notifications as read
   */
  markMultipleAsRead(notificationIds: string[]): Observable<void> {
    const notificationHub = this.signalRService.notificationHub;

    if (notificationHub) {
      notificationHub.invoke('MarkMultipleAsRead', notificationIds)
        .catch(error => console.error('Failed to mark multiple notifications as read via SignalR', error));
    }

    return this.http.post<void>(`${this.baseUrl}/api/app/notification/mark-multiple-as-read`, notificationIds)
      .pipe(
        map(() => {
          // Update local counts
          const currentCount = this.unreadCount$.value;
          const newCount = Math.max(0, currentCount - notificationIds.length);
          this.unreadCount$.next(newCount);
        })
      );
  }

  /**
   * Mark all notifications as read
   */
  markAllAsRead(): Observable<void> {
    const notificationHub = this.signalRService.notificationHub;

    if (notificationHub) {
      notificationHub.invoke('MarkAllAsRead')
        .catch(error => console.error('Failed to mark all notifications as read via SignalR', error));
    }

    return this.http.post<void>(`${this.baseUrl}/api/app/notification/mark-all-as-read`, {})
      .pipe(
        map(() => {
          this.unreadCount$.next(0);
        })
      );
  }

  /**
   * Get notification statistics
   */
  getNotificationStats(): Observable<NotificationStatsDto> {
    return this.http.get<NotificationStatsDto>(`${this.baseUrl}/api/app/notification/stats`);
  }

  /**
   * Delete a notification
   */
  deleteNotification(notificationId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/app/notification/${notificationId}`);
  }

  /**
   * Delete expired notifications (admin/system function)
   */
  deleteExpiredNotifications(): Observable<number> {
    return this.http.delete<number>(`${this.baseUrl}/api/app/notification/expired`);
  }

  /**
   * Get current unread count from server
   */
  refreshUnreadCount(): Observable<number> {
    return this.http.get<number>(`${this.baseUrl}/api/app/notification/unread-count`)
      .pipe(
        map(count => {
          this.unreadCount$.next(count);
          return count;
        })
      );
  }

  /**
   * Build HTTP params from input object
   */
  private buildHttpParams(input: any): HttpParams {
    let params = new HttpParams();

    Object.keys(input).forEach(key => {
      if (input[key] !== undefined && input[key] !== null) {
        params = params.set(key, input[key].toString());
      }
    });

    return params;
  }
}
