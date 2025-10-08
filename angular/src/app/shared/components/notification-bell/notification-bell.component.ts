import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NotificationService, NotificationMessage } from '../../../services/notification.service';

@Component({
  selector: 'app-notification-bell',
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.scss'],
  standalone: false
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  notifications: NotificationMessage[] = [];
  unreadCount = 0;
  private destroy$ = new Subject<void>();

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    // Subscribe to incoming notifications
    this.notificationService.notifications
      .pipe(takeUntil(this.destroy$))
      .subscribe(notification => {
        this.notifications.unshift(notification);
        this.unreadCount = this.notificationService.unreadNotificationCount;

        // Keep only last 20 notifications
        if (this.notifications.length > 20) {
          this.notifications = this.notifications.slice(0, 20);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  togglePanel(event: Event): void {
    // Panel toggle is handled by p-overlayPanel
  }

  onNotificationClick(notification: NotificationMessage): void {
    this.notificationService.markAsRead(notification.id);

    if (notification.actionUrl) {
      // Navigate to action URL if provided
      window.location.href = notification.actionUrl;
    }
  }

  dismissNotification(notification: NotificationMessage, event: Event): void {
    event.stopPropagation();

    const index = this.notifications.indexOf(notification);
    if (index > -1) {
      this.notifications.splice(index, 1);
      this.notificationService.markAsRead(notification.id);
    }
  }

  clearAll(): void {
    this.notifications.forEach(n => this.notificationService.markAsRead(n.id));
    this.notifications = [];
    this.notificationService.clearUnreadCount();
  }

  getIconClass(severity: string): string {
    switch (severity) {
      case 'success':
        return 'pi pi-check-circle text-green-500';
      case 'error':
        return 'pi pi-times-circle text-red-500';
      case 'warn':
        return 'pi pi-exclamation-triangle text-orange-500';
      default:
        return 'pi pi-info-circle text-blue-500';
    }
  }
}
