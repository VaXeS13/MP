import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { GetNotificationsInput, NotificationListDto, NotificationStatsDto } from '../contracts/notifications/models';
import type { NotificationMessageDto } from '../contracts/signal-r/models';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  apiName = 'Default';
  

  delete = (notificationId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: '/api/app/notification',
      params: { notificationId },
    },
    { apiName: this.apiName,...config });
  

  deleteExpiredNotifications = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, number>({
      method: 'DELETE',
      url: '/api/app/notification/expired-notifications',
    },
    { apiName: this.apiName,...config });
  

  getAll = (input: GetNotificationsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotificationListDto>({
      method: 'GET',
      url: '/api/app/notification',
      params: { isRead: input.isRead, severity: input.severity, type: input.type, startDate: input.startDate, endDate: input.endDate, includeExpired: input.includeExpired, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetNotificationsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotificationListDto>({
      method: 'GET',
      url: '/api/app/notification',
      params: { isRead: input.isRead, severity: input.severity, type: input.type, startDate: input.startDate, endDate: input.endDate, includeExpired: input.includeExpired, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getStats = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotificationStatsDto>({
      method: 'GET',
      url: '/api/app/notification/stats',
    },
    { apiName: this.apiName,...config });
  

  getUnread = (input: GetNotificationsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, NotificationListDto>({
      method: 'GET',
      url: '/api/app/notification/unread',
      params: { isRead: input.isRead, severity: input.severity, type: input.type, startDate: input.startDate, endDate: input.endDate, includeExpired: input.includeExpired, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getUnreadCount = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, number>({
      method: 'GET',
      url: '/api/app/notification/unread-count',
    },
    { apiName: this.apiName,...config });
  

  markAllAsRead = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/notification/mark-all-as-read',
    },
    { apiName: this.apiName,...config });
  

  markAsRead = (notificationId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/notification/mark-as-read/${notificationId}`,
    },
    { apiName: this.apiName,...config });
  

  markMultipleAsRead = (notificationIds: string[], config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/notification/mark-multiple-as-read',
      body: notificationIds,
    },
    { apiName: this.apiName,...config });
  

  sendToTenant = (notification: NotificationMessageDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/notification/send-to-tenant',
      body: notification,
    },
    { apiName: this.apiName,...config });
  

  sendToUser = (userId: string, notification: NotificationMessageDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/notification/send-to-user/${userId}`,
      body: notification,
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
