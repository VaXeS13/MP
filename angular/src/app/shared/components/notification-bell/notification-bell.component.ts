import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { OverlayPanel } from 'primeng/overlaypanel';
import { NotificationService } from '../../../services/notification.service';

@Component({
  selector: 'app-notification-bell',
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.scss'],
  standalone: false
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  @ViewChild('notificationPanel') notificationPanel!: OverlayPanel;

  unreadCount = 0;
  private destroy$ = new Subject<void>();

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    // Subscribe to unread count changes
    this.notificationService.unreadNotificationCount
      .pipe(takeUntil(this.destroy$))
      .subscribe(count => {
        this.unreadCount = count;
      });

    // Initialize unread count
    this.notificationService.refreshUnreadCount()
      .pipe(takeUntil(this.destroy$))
      .subscribe();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  togglePanel(event: Event): void {
    this.notificationPanel.toggle(event);
  }

  hidePanel(): void {
    this.notificationPanel.hide();
  }

  // Icon styling methods
  getBellIconClass(): string {
    return this.unreadCount > 0 ? 'pi pi-bell text-primary' : 'pi pi-bell text-color-secondary';
  }

  getBadgeSeverity(): string {
    if (this.unreadCount > 10) {
      return 'danger';
    } else if (this.unreadCount > 5) {
      return 'warning';
    } else if (this.unreadCount > 0) {
      return 'info';
    }
    return 'secondary';
  }

  getBadgeValue(): string {
    if (this.unreadCount > 99) {
      return '99+';
    }
    return this.unreadCount.toString();
  }
}
