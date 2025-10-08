import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { ChatService } from '../../../services/chat.service';

@Component({
  selector: 'app-chat-icon',
  standalone: false,
  templateUrl: './chat-icon.component.html',
  styleUrl: './chat-icon.component.scss'
})
export class ChatIconComponent implements OnInit, OnDestroy {
  unreadCount = 0;
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private chatService: ChatService
  ) {}

  ngOnInit(): void {
    // Update unread count immediately
    this.updateUnreadCount();

    // Subscribe to conversations updates from ChatService
    this.chatService.onConversationsUpdated
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.updateUnreadCount();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  navigateToChat(): void {
    this.router.navigate(['/chat']);
  }

  private updateUnreadCount(): void {
    this.unreadCount = this.chatService.getUnreadCount();
  }
}
