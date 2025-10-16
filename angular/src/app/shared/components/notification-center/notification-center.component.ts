import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { Subject, takeUntil, merge, of } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { ProgressBarModule } from 'primeng/progressbar';
import { BadgeModule } from 'primeng/badge';
import { TooltipModule } from 'primeng/tooltip';
import { InputTextModule } from 'primeng/inputtext';
import { DropdownModule } from 'primeng/dropdown';
import { PaginatorModule } from 'primeng/paginator';
import { TabViewModule } from 'primeng/tabview';
import { RippleModule } from 'primeng/ripple';
import { MessageModule } from 'primeng/message';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  NotificationService,
  NotificationDto,
  NotificationListDto,
  GetNotificationsInput,
  NotificationStatsDto,
  DEFAULT_STATS
} from '../../../services/notification.service';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ButtonModule,
    ProgressBarModule,
    BadgeModule,
    TooltipModule,
    InputTextModule,
    DropdownModule,
    PaginatorModule,
    TabViewModule,
    RippleModule,
    MessageModule
  ],
  templateUrl: './notification-center.component.html',
  styleUrls: ['./notification-center.component.scss']
})
export class NotificationCenterComponent implements OnInit, OnDestroy {
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  // Tab management
  activeTabIndex = 0;

  // Notification data
  unreadNotifications: NotificationDto[] = [];
  allNotifications: NotificationDto[] = [];

  // Pagination
  unreadTotalCount = 0;
  allTotalCount = 0;
  currentPage = 0;
  pageSize = 10;
  loading = false;

  // Search and filter
  searchTerm = '';
  selectedSeverity: any = null;
  severityOptions = [
    { label: 'Wszystkie', value: null },
    { label: 'Info', value: 'info' },
    { label: 'Sukces', value: 'success' },
    { label: 'Ostrzeżenie', value: 'warning' },
    { label: 'Błąd', value: 'error' }
  ];

  // Stats
  stats: NotificationStatsDto = { ...DEFAULT_STATS };

  // State management
  selectedNotifications: string[] = [];
  private destroy$ = new Subject<void>();
  private searchSubject$ = new Subject<string>();

  constructor(private notificationService: NotificationService) {}

  ngOnInit(): void {
    this.initializeSearch();
    this.loadInitialData();
    this.subscribeToRealTimeUpdates();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Initialize search with debouncing
  private initializeSearch(): void {
    this.searchSubject$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(term => {
      this.searchTerm = term;
      this.refreshCurrentTab();
    });
  }

  // Load initial data
  private loadInitialData(): void {
    this.loadUnreadNotifications();
    this.loadAllNotifications();
    this.loadNotificationStats();
  }

  // Subscribe to real-time updates from SignalR
  private subscribeToRealTimeUpdates(): void {
    // Listen to new notifications
    this.notificationService.notifications.pipe(
      takeUntil(this.destroy$)
    ).subscribe(newNotification => {
      // Refresh current tab to show new notification
      this.refreshCurrentTab();
      this.loadNotificationStats(); // Update stats
    });

    // Listen to unread count changes
    this.notificationService.unreadNotificationCount.pipe(
      takeUntil(this.destroy$)
    ).subscribe(count => {
      // Could update UI if needed
    });
  }

  // Tab switching
  onTabChange(event: any): void {
    this.activeTabIndex = event.index;
    this.currentPage = 0;
    this.refreshCurrentTab();
  }

  // Refresh current active tab
  refreshCurrentTab(): void {
    if (this.activeTabIndex === 0) {
      this.loadUnreadNotifications();
    } else {
      this.loadAllNotifications();
    }
  }

  // Load unread notifications
  private loadUnreadNotifications(page: number = 0): void {
    this.loading = true;

    const input: GetNotificationsInput = {
      isRead: false,
      skipCount: page * this.pageSize,
      maxResultCount: this.pageSize,
      sorting: 'CreationTime DESC'
    };

    this.applyFilters(input);

    this.notificationService.getUnreadNotifications(input).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response: NotificationListDto) => {
        this.unreadNotifications = response.items;
        this.unreadTotalCount = response.totalCount;
        this.loading = false;

        // Automatically mark visible unread notifications as read after a short delay
        // to ensure user has actually seen them
        this.autoMarkVisibleAsRead();
      },
      error: (error) => {
        console.error('Failed to load unread notifications:', error);
        this.loading = false;
      }
    });
  }

  // Load all notifications
  private loadAllNotifications(page: number = 0): void {
    this.loading = true;

    const input: GetNotificationsInput = {
      skipCount: page * this.pageSize,
      maxResultCount: this.pageSize,
      sorting: 'CreationTime DESC'
    };

    this.applyFilters(input);

    this.notificationService.getAllNotifications(input).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response: NotificationListDto) => {
        this.allNotifications = response.items;
        this.allTotalCount = response.totalCount;
        this.loading = false;
      },
      error: (error) => {
        console.error('Failed to load all notifications:', error);
        this.loading = false;
      }
    });
  }

  // Load notification statistics
  private loadNotificationStats(): void {
    this.notificationService.getNotificationStats().pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (stats: NotificationStatsDto) => {
        this.stats = stats;
      },
      error: (error) => {
        console.error('Failed to load notification stats:', error);
      }
    });
  }

  // Apply filters to input
  private applyFilters(input: GetNotificationsInput): void {
    if (this.selectedSeverity?.value) {
      input.severity = this.selectedSeverity.value;
    }

    if (this.searchTerm) {
      // This would need backend support for text search
      // For now, filtering is done client-side
    }
  }

  // Pagination
  onPageChange(event: any): void {
    this.currentPage = event.page;
    if (this.activeTabIndex === 0) {
      this.loadUnreadNotifications(this.currentPage);
    } else {
      this.loadAllNotifications(this.currentPage);
    }
  }

  // Search
  onSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchSubject$.next(target.value);
  }

  // Filter by severity
  onSeverityChange(): void {
    this.currentPage = 0;
    this.refreshCurrentTab();
  }

  // Notification actions
  onNotificationClick(notification: NotificationDto): void {
    if (!notification.isRead) {
      this.markAsRead(notification.id);
    }

    if (notification.actionUrl) {
      window.location.href = notification.actionUrl;
    }
  }

  onNotificationSelect(notificationId: string, event: Event): void {
    event.stopPropagation();

    const index = this.selectedNotifications.indexOf(notificationId);
    if (index > -1) {
      this.selectedNotifications.splice(index, 1);
    } else {
      this.selectedNotifications.push(notificationId);
    }
  }

  // Mark as read
  async markAsRead(notificationId: string): Promise<void> {
    await this.notificationService.markAsRead(notificationId);

    // Update local state
    const updateNotification = (notifications: NotificationDto[]) => {
      const notification = notifications.find(n => n.id === notificationId);
      if (notification) {
        notification.isRead = true;
        notification.readAt = new Date().toISOString();
      }
    };

    updateNotification(this.unreadNotifications);
    updateNotification(this.allNotifications);

    // Refresh stats after marking as read
    this.loadNotificationStats();
  }

  // Mark selected as read
  markSelectedAsRead(): void {
    if (this.selectedNotifications.length === 0) return;

    this.notificationService.markMultipleAsRead(this.selectedNotifications).subscribe(() => {
      // Update local state
      const updateNotifications = (notifications: NotificationDto[]) => {
        notifications.forEach(notification => {
          if (this.selectedNotifications.includes(notification.id)) {
            notification.isRead = true;
            notification.readAt = new Date().toISOString();
          }
        });
      };

      updateNotifications(this.unreadNotifications);
      updateNotifications(this.allNotifications);
      this.selectedNotifications = [];

      // Refresh stats after marking selected as read
      this.loadNotificationStats();
    });
  }

  // Mark all as read
  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      // Update local state
      this.unreadNotifications.forEach(notification => {
        notification.isRead = true;
        notification.readAt = new Date().toISOString();
      });
      this.allNotifications.forEach(notification => {
        notification.isRead = true;
        notification.readAt = new Date().toISOString();
      });

      // Refresh stats after marking all as read
      this.loadNotificationStats();
    });
  }

  // Automatically mark visible unread notifications as read
  private autoMarkVisibleAsRead(): void {
    // Only auto-mark in unread tab
    if (this.activeTabIndex !== 0) {
      return;
    }

    // Collect IDs of currently visible unread notifications
    const unreadIds = this.currentNotifications
      .filter(notification => !notification.isRead)
      .map(notification => notification.id);

    if (unreadIds.length === 0) {
      return;
    }

    // Wait 1.5 seconds before marking as read to ensure user has seen them
    setTimeout(() => {
      this.notificationService.markMultipleAsRead(unreadIds).subscribe({
        next: () => {
          console.log(`Auto-marked ${unreadIds.length} notifications as read`);

          // Update local state
          const updateNotifications = (notifications: NotificationDto[]) => {
            notifications.forEach(notification => {
              if (unreadIds.includes(notification.id)) {
                notification.isRead = true;
                notification.readAt = new Date().toISOString();
              }
            });
          };

          updateNotifications(this.unreadNotifications);
          updateNotifications(this.allNotifications);

          // Refresh stats to update unread count
          this.loadNotificationStats();
        },
        error: (error) => {
          console.error('Failed to auto-mark notifications as read:', error);
        }
      });
    }, 1500); // 1.5 second delay
  }

  // Delete notification
  deleteNotification(notificationId: string): void {
    this.notificationService.deleteNotification(notificationId).subscribe(() => {
      // Update local state
      this.unreadNotifications = this.unreadNotifications.filter(n => n.id !== notificationId);
      this.allNotifications = this.allNotifications.filter(n => n.id !== notificationId);
      this.selectedNotifications = this.selectedNotifications.filter(id => id !== notificationId);
      this.loadNotificationStats(); // Update stats
    });
  }

  // Utility methods
  getSeverityClass(severity: string): string {
    switch (severity) {
      case 'success':
        return 'text-green-500';
      case 'warning':
        return 'text-orange-500';
      case 'error':
        return 'text-red-500';
      default:
        return 'text-blue-500';
    }
  }

  getSeverityIcon(severity: string): string {
    switch (severity) {
      case 'success':
        return 'pi pi-check-circle';
      case 'warning':
        return 'pi pi-exclamation-triangle';
      case 'error':
        return 'pi pi-times-circle';
      default:
        return 'pi pi-info-circle';
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffHours < 1) {
      return 'Przed chwilą';
    } else if (diffHours < 24) {
      return `${diffHours} godz. temu`;
    } else if (diffDays === 1) {
      return 'Wczoraj';
    } else if (diffDays < 7) {
      return `${diffDays} dni temu`;
    } else {
      return date.toLocaleDateString('pl-PL');
    }
  }

  // Get current notifications for display
  get currentNotifications(): NotificationDto[] {
    const notifications = this.activeTabIndex === 0 ? this.unreadNotifications : this.allNotifications;

    if (!this.searchTerm) {
      return notifications;
    }

    // Client-side filtering by search term
    const searchLower = this.searchTerm.toLowerCase();
    return notifications.filter(notification =>
      notification.title.toLowerCase().includes(searchLower) ||
      notification.message.toLowerCase().includes(searchLower) ||
      notification.type.toLowerCase().includes(searchLower)
    );
  }

  get currentTotalCount(): number {
    return this.activeTabIndex === 0 ? this.unreadTotalCount : this.allTotalCount;
  }

  // Check if any notifications are selected
  get hasSelection(): boolean {
    return this.selectedNotifications.length > 0;
  }

  // Check if there are any unread notifications in current view
  get hasUnreadInCurrentView(): boolean {
    return this.currentNotifications.some(n => !n.isRead);
  }

  // Track by function for ngFor
  trackByNotificationId(index: number, notification: NotificationDto): string {
    return notification.id;
  }
}