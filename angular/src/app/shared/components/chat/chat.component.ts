import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil, debounceTime } from 'rxjs/operators';
import { ChatService, ChatMessage, ChatConversation } from '../../../services/chat.service';
import { ConfigStateService, PermissionService } from '@abp/ng.core';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { CustomerUserDto } from '../../../proxy/application/contracts/chat/models';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    BadgeModule,
    DialogModule,
    InputTextModule,
    TooltipModule
  ]
})
export class ChatComponent implements OnInit, OnDestroy {
  @ViewChild('messageContainer') private messageContainer?: ElementRef;
  @ViewChild('messageInput') private messageInput?: ElementRef;

  conversations: ChatConversation[] = [];
  selectedConversation?: ChatConversation;
  messages: ChatMessage[] = [];
  newMessage = '';
  isTyping = false;
  otherUserTyping = false;
  currentUserId?: string;
  isSupport = false;

  showUserSelectDialog = false;
  allCustomers: CustomerUserDto[] = [];
  userSearchQuery = '';

  private destroy$ = new Subject<void>();
  private typingSubject$ = new Subject<string>();

  constructor(
    private chatService: ChatService,
    private configState: ConfigStateService,
    private permissionService: PermissionService,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.currentUserId = this.configState.getOne('currentUser')?.id;

    if (!this.currentUserId) {
      console.error('Current user ID not found');
      return;
    }

    // Check if user has support permissions
    this.isSupport = this.permissionService.getGrantedPolicy('MP.Chat.ManageCustomerChats');

    // Initialize chat service with current user ID
    this.chatService.initialize(this.currentUserId);

    // Subscribe to incoming messages for UI updates (add to messages array, scroll)
    this.chatService.onMessageReceived
      .pipe(takeUntil(this.destroy$))
      .subscribe(message => {
        this.handleIncomingMessageUI(message);
      });

    // Subscribe to read receipts for UI updates
    this.chatService.onMessageReadReceipt
      .pipe(takeUntil(this.destroy$))
      .subscribe(receipt => {
        this.handleReadReceipt(receipt.messageId, receipt.readAt);
      });

    // Subscribe to typing indicators
    this.chatService.onUserTyping
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        if (this.selectedConversation?.userId === data.userId) {
          this.otherUserTyping = data.isTyping;
        }
      });

    // Subscribe to conversations updates
    this.chatService.onConversationsUpdated
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.conversations = this.chatService.getConversations();
      });

    // Debounce typing indicator
    this.typingSubject$
      .pipe(
        debounceTime(300),
        takeUntil(this.destroy$)
      )
      .subscribe(message => {
        if (this.selectedConversation) {
          const isTyping = message.length > 0;
          if (this.isTyping !== isTyping) {
            this.isTyping = isTyping;
            this.chatService.sendTypingIndicator(this.selectedConversation.userId, isTyping);
          }
        }
      });

    // Load conversations
    this.loadConversationsAsync();

    // Handle query params for automatic conversation opening
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const userId = params['userId'];
        if (userId && this.conversations.length > 0) {
          this.openConversationByUserId(userId);
        }
      });
  }

  async loadConversationsAsync(): Promise<void> {
    try {
      // Load conversations from API (already loaded by ChatService.initialize)
      await this.chatService.loadConversations();

      // Get conversations from ChatService
      this.conversations = this.chatService.getConversations();

      // If no conversations (customer), load available support users
      if (this.conversations.length === 0) {
        const supportUsers = await this.chatService.getAvailableSupportUsers();

        // Create conversations from support users
        this.conversations = supportUsers.map(user => ({
          userId: user.id || '',
          userName: user.name || 'Support',
          userEmail: user.email || '',
          unreadCount: 0,
          lastMessage: undefined
        }));
      }

      // Check for userId in query params after loading conversations
      const userId = this.route.snapshot.queryParams['userId'];
      if (userId) {
        await this.openConversationByUserId(userId);
      }
    } catch (error) {
      console.error('Error loading conversations:', error);
    }
  }

  private async openConversationByUserId(userId: string): Promise<void> {
    // Find existing conversation
    let conversation = this.conversations.find(c => c.userId === userId);

    if (!conversation) {
      // Conversation doesn't exist, try to load user info and create it
      try {
        // For support, fetch customer info
        if (this.isSupport) {
          const customers = await this.chatService.getAllCustomers();
          const customer = customers.find(c => c.id === userId);
          if (customer) {
            conversation = {
              userId: customer.id || '',
              userName: customer.name || 'Customer',
              userEmail: customer.email || '',
              unreadCount: 0,
              lastMessage: undefined
            };
            this.conversations.unshift(conversation);
          }
        } else {
          // For customers, fetch support user info
          const supportUsers = await this.chatService.getAvailableSupportUsers();
          const supportUser = supportUsers.find(u => u.id === userId);
          if (supportUser) {
            conversation = {
              userId: supportUser.id || '',
              userName: supportUser.name || 'Support',
              userEmail: supportUser.email || '',
              unreadCount: 0,
              lastMessage: undefined
            };
            this.conversations.unshift(conversation);
          }
        }
      } catch (error) {
        console.error('Error loading user info:', error);
        return;
      }
    }

    // Select the conversation if found or created
    if (conversation) {
      await this.selectConversation(conversation);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }


  async selectConversation(conversation: ChatConversation): Promise<void> {
    this.selectedConversation = conversation;

    // Notify ChatService about selected conversation
    this.chatService.setSelectedConversation(conversation.userId);

    // Load message history from backend
    try {
      this.messages = await this.chatService.loadMessageHistory(conversation.userId);

      // Mark all loaded messages as processed to avoid duplicate counting
      this.chatService.markLoadedMessagesAsProcessed(this.messages);

      // Mark all messages from this user as read
      await this.chatService.markAsRead(conversation.userId);

      // Clear unread count
      this.chatService.clearConversationUnread(conversation.userId);
      conversation.unreadCount = 0;

      // Scroll to bottom after loading messages
      setTimeout(() => this.scrollToBottom(), 100);
    } catch (error) {
      console.error('Error loading messages:', error);
      this.messages = [];
    }
  }

  async sendMessage(): Promise<void> {
    if (!this.newMessage.trim() || !this.selectedConversation) {
      return;
    }

    try {
      await this.chatService.sendMessage(
        this.selectedConversation.userId,
        this.newMessage.trim()
      );

      // Clear input - message will be added via MessageSent event
      this.newMessage = '';
      this.typingSubject$.next('');
    } catch (error) {
      console.error('Error sending message:', error);
    }
  }

  onMessageInput(): void {
    this.typingSubject$.next(this.newMessage);
  }

  handleKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private handleIncomingMessageUI(message: ChatMessage): void {
    // UI logic only: Add message to current conversation and scroll
    // unreadCount is managed globally by ChatService

    // Check if this is a message sent by current user
    const isSentByMe = message.senderId === this.currentUserId;

    // Find the OTHER user in the conversation
    const otherUserId = isSentByMe ? message.receiverId : message.senderId;

    // Add to messages if conversation is selected and we don't already have this message
    if (this.selectedConversation?.userId === otherUserId) {
      // Check if message already exists to prevent duplicates
      const existingMessage = this.messages.find(m => m.id === message.id);
      if (!existingMessage) {
        this.messages.push(message);
      }

      // Scroll to bottom
      setTimeout(() => this.scrollToBottom(), 100);
    }
  }

  private handleReadReceipt(messageId: string, readAt: Date): void {
    const message = this.messages.find(m => m.id === messageId);
    if (message) {
      message.isRead = true;
    }
  }

  private scrollToBottom(): void {
    if (this.messageContainer) {
      const element = this.messageContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }

  isSentByMe(message: ChatMessage): boolean {
    return message.senderId === this.currentUserId;
  }

  getTotalUnreadCount(): number {
    return this.chatService.getUnreadCount();
  }

  get filteredCustomers(): CustomerUserDto[] {
    if (!this.userSearchQuery.trim()) {
      return this.allCustomers;
    }

    const query = this.userSearchQuery.toLowerCase();
    return this.allCustomers.filter(customer =>
      customer.name?.toLowerCase().includes(query) ||
      customer.email?.toLowerCase().includes(query)
    );
  }

  async showNewMessageDialog(): Promise<void> {
    try {
      this.allCustomers = await this.chatService.getAllCustomers();
      this.userSearchQuery = '';
      this.showUserSelectDialog = true;
    } catch (error) {
      console.error('Error loading customers:', error);
    }
  }

  async startConversationWithUser(user: CustomerUserDto): Promise<void> {
    this.showUserSelectDialog = false;

    // Check if conversation already exists
    let conversation = this.conversations.find(c => c.userId === user.id);

    if (!conversation) {
      // Create new conversation
      conversation = {
        userId: user.id || '',
        userName: user.name || 'Customer',
        userEmail: user.email || '',
        unreadCount: 0,
        lastMessage: undefined
      };
      this.conversations.unshift(conversation);
    }

    // Select the conversation
    await this.selectConversation(conversation);
  }

  trackByConversationUserId(index: number, conversation: ChatConversation): string {
    return conversation.userId;
  }

  trackByMessageId(index: number, message: ChatMessage): string {
    return message.id;
  }

  trackByCustomerId(index: number, customer: CustomerUserDto): string {
    return customer.id || index.toString();
  }
}
