import type { EntityDto, PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import type { NotificationSeverity } from './notification-severity.enum';
import type { NotificationDto } from './models';

export interface GetNotificationsInput extends PagedAndSortedResultRequestDto {
  isRead?: boolean;
  severity?: NotificationSeverity;
  type?: string;
  startDate?: string;
  endDate?: string;
  includeExpired: boolean;
}

export interface NotificationDto extends EntityDto<string> {
  type?: string;
  title?: string;
  message?: string;
  severity?: NotificationSeverity;
  isRead: boolean;
  readAt?: string;
  actionUrl?: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  creationTime?: string;
  expiresAt?: string;
}

export interface NotificationListDto extends PagedResultDto<NotificationDto> {
  unreadCount: number;
}

export interface NotificationStatsDto {
  totalCount: number;
  unreadCount: number;
  readCount: number;
  expiredCount: number;
}
