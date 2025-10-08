import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { trigger, transition, style, animate } from '@angular/animations';
import { ChatService, ChatMessage } from '../../../services/chat.service';
import { ConfigStateService } from '@abp/ng.core';

interface ChatNotification {
  id: string;
  senderId: string;
  senderName: string;
  message: string;
  timestamp: Date;
}

@Component({
  selector: 'app-chat-notification',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chat-notification.component.html',
  styleUrls: ['./chat-notification.component.scss'],
  animations: [
    trigger('slideIn', [
      transition(':enter', [
        style({ transform: 'translateX(400px)', opacity: 0 }),
        animate('300ms ease-out', style({ transform: 'translateX(0)', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('200ms ease-in', style({ transform: 'translateX(400px)', opacity: 0 }))
      ])
    ])
  ]
})
export class ChatNotificationComponent implements OnInit, OnDestroy {
  notifications: ChatNotification[] = [];
  currentUserId?: string;
  private destroy$ = new Subject<void>();
  private readonly MAX_NOTIFICATIONS = 3;
  private readonly AUTO_DISMISS_TIME = 5000;

  constructor(
    private chatService: ChatService,
    private configState: ConfigStateService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.configState.getOne('currentUser')?.id;

    // Subscribe to incoming messages
    this.chatService.onMessageReceived
      .pipe(takeUntil(this.destroy$))
      .subscribe(message => {
        this.handleIncomingMessage(message);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private handleIncomingMessage(message: ChatMessage): void {
    // Don't show notification for own messages
    if (message.senderId === this.currentUserId) {
      return;
    }

    // Don't show notification if user is already in chat
    if (this.router.url.includes('/chat')) {
      return;
    }

    // Check if notification already exists (prevent duplicates)
    const existingNotification = this.notifications.find(n => n.id === message.id);
    if (existingNotification) {
      return;
    }

    // Create notification
    const notification: ChatNotification = {
      id: message.id,
      senderId: message.senderId,
      senderName: message.senderName,
      message: this.truncateMessage(message.message),
      timestamp: message.sentAt
    };

    // Add to notifications (limit to MAX_NOTIFICATIONS)
    this.notifications.push(notification);
    if (this.notifications.length > this.MAX_NOTIFICATIONS) {
      this.notifications.shift();
    }

    // Auto dismiss after timeout
    setTimeout(() => {
      this.dismissNotification(notification.id);
    }, this.AUTO_DISMISS_TIME);
  }

  private truncateMessage(message: string): string {
    const MAX_LENGTH = 50;
    if (message.length > MAX_LENGTH) {
      return message.substring(0, MAX_LENGTH) + '...';
    }
    return message;
  }

  dismissNotification(id: string): void {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  async openChat(notification: ChatNotification): Promise<void> {
    // Navigate to chat with the sender
    await this.router.navigate(['/chat'], {
      queryParams: { userId: notification.senderId }
    });

    // Dismiss the notification
    this.dismissNotification(notification.id);
  }

  trackByNotificationId(index: number, notification: ChatNotification): string {
    return notification.id;
  }
}
