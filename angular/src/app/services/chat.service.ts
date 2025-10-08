import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';
import { SignalRService } from './signalr.service';
import { ChatService as ChatApiService } from '../proxy/application/chat/chat.service';
import { SupportUserDto, CustomerUserDto } from '../proxy/application/contracts/chat/models';

export interface ChatMessage {
  id: string;
  senderId: string;
  senderName: string;
  receiverId: string;
  message: string;
  sentAt: Date;
  isRead: boolean;
}

export interface ChatConversation {
  userId: string;
  userName: string;
  userEmail: string;
  lastMessage?: ChatMessage;
  unreadCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private messageReceived$ = new Subject<ChatMessage>();
  private messageReadReceipt$ = new Subject<{ messageId: string; readAt: Date }>();
  private userTyping$ = new Subject<{ userId: string; isTyping: boolean }>();
  private conversationsUpdated$ = new Subject<void>();

  public readonly onMessageReceived: Observable<ChatMessage> = this.messageReceived$.asObservable();
  public readonly onMessageReadReceipt: Observable<{ messageId: string; readAt: Date }> = this.messageReadReceipt$.asObservable();
  public readonly onUserTyping: Observable<{ userId: string; isTyping: boolean }> = this.userTyping$.asObservable();
  public readonly onConversationsUpdated: Observable<void> = this.conversationsUpdated$.asObservable();

  private conversations: Map<string, ChatConversation> = new Map();
  private processedMessageIds = new Set<string>();
  private currentUserId?: string;
  private selectedConversationUserId?: string;

  constructor(
    private signalRService: SignalRService,
    private chatApiService: ChatApiService
  ) {}

  initialize(currentUserId: string): void {
    this.currentUserId = currentUserId;
    const chatHub = this.signalRService.chatHub;

    if (!chatHub) {
      console.error('Chat hub is not initialized');
      return;
    }

    // Listen for incoming messages
    chatHub.on('ReceiveMessage', (message: any) => {
      const chatMessage: ChatMessage = {
        id: message.id,
        senderId: message.senderId,
        senderName: message.senderName,
        receiverId: message.receiverId,
        message: message.message,
        sentAt: new Date(message.sentAt),
        isRead: message.isRead
      };

      this.handleIncomingMessage(chatMessage);
      this.messageReceived$.next(chatMessage);
    });

    // Listen for message sent confirmation
    chatHub.on('MessageSent', (message: any) => {
      const chatMessage: ChatMessage = {
        id: message.id,
        senderId: message.senderId,
        senderName: message.senderName,
        receiverId: message.receiverId,
        message: message.message,
        sentAt: new Date(message.sentAt),
        isRead: message.isRead
      };

      this.handleIncomingMessage(chatMessage);
      this.messageReceived$.next(chatMessage);
    });

    // Listen for read receipts
    chatHub.on('MessageRead', (data: any) => {
      this.handleMessageRead(data.senderId);
      this.messageReadReceipt$.next({
        messageId: data.messageId,
        readAt: new Date(data.readAt)
      });
    });

    // Listen for typing indicators
    chatHub.on('UserTyping', (data: any) => {
      this.userTyping$.next({
        userId: data.userId,
        isTyping: data.isTyping
      });
    });

    // Load initial conversations to populate unread counts
    this.loadConversations().then(conversations => {
      conversations.forEach(conv => {
        this.conversations.set(conv.userId, conv);
        // Mark all loaded messages as processed
        if (conv.lastMessage) {
          this.processedMessageIds.add(conv.lastMessage.id);
        }
      });
      this.conversationsUpdated$.next();
    }).catch(error => {
      console.error('Failed to load initial conversations', error);
    });
  }

  async sendMessage(receiverId: string, message: string): Promise<void> {
    const chatHub = this.signalRService.chatHub;

    if (!chatHub) {
      throw new Error('Chat hub is not connected');
    }

    try {
      await chatHub.invoke('SendMessage', receiverId, message);
    } catch (error) {
      console.error('Error sending chat message:', error);
      throw error;
    }
  }

  async markAsRead(senderId: string): Promise<void> {
    const chatHub = this.signalRService.chatHub;

    if (!chatHub) {
      return;
    }

    try {
      await chatHub.invoke('MarkMessageAsRead', senderId);
    } catch (error) {
      console.error('Error marking message as read:', error);
    }
  }

  async sendTypingIndicator(receiverId: string, isTyping: boolean): Promise<void> {
    const chatHub = this.signalRService.chatHub;

    if (!chatHub) {
      return;
    }

    try {
      await chatHub.invoke('SendTypingIndicator', receiverId, isTyping);
    } catch (error) {
      console.error('Error sending typing indicator:', error);
    }
  }


  getConversations(): ChatConversation[] {
    return Array.from(this.conversations.values());
  }

  async loadConversations(): Promise<ChatConversation[]> {
    try {
      const conversations = await this.chatApiService.getMyConversations().toPromise();

      // Convert API DTOs to internal format
      const chatConversations: ChatConversation[] = conversations?.map(conv => ({
        userId: conv.userId || '',
        userName: conv.userName || '',
        userEmail: conv.userEmail || '',
        unreadCount: conv.unreadCount || 0,
        lastMessage: conv.lastMessage ? {
          id: conv.lastMessage.id || '',
          senderId: conv.lastMessage.senderId || '',
          senderName: conv.lastMessage.senderName || '',
          receiverId: conv.lastMessage.receiverId || '',
          message: conv.lastMessage.message || '',
          sentAt: conv.lastMessage.sentAt ? new Date(conv.lastMessage.sentAt) : new Date(),
          isRead: conv.lastMessage.isRead || false
        } : undefined
      })) || [];

      // Update local conversations map
      chatConversations.forEach(conv => {
        this.conversations.set(conv.userId, conv);
      });

      return chatConversations;
    } catch (error) {
      console.error('Error loading conversations:', error);
      return [];
    }
  }

  async getAvailableSupportUsers(): Promise<SupportUserDto[]> {
    try {
      const users = await this.chatApiService.getAvailableSupportUsers().toPromise();
      return users || [];
    } catch (error) {
      console.error('Error loading support users:', error);
      return [];
    }
  }

  async getAllCustomers(): Promise<CustomerUserDto[]> {
    try {
      const customers = await this.chatApiService.getAllCustomers().toPromise();
      return customers || [];
    } catch (error) {
      console.error('Error loading customers:', error);
      return [];
    }
  }

  async loadMessageHistory(otherUserId: string): Promise<ChatMessage[]> {
    try {
      const messages = await this.chatApiService.getMessages(otherUserId).toPromise();

      return messages?.map(msg => ({
        id: msg.id || '',
        senderId: msg.senderId || '',
        senderName: msg.senderName || '',
        receiverId: msg.receiverId || '',
        message: msg.message || '',
        sentAt: msg.sentAt ? new Date(msg.sentAt) : new Date(),
        isRead: msg.isRead || false
      })) || [];
    } catch (error) {
      console.error('Error loading message history:', error);
      return [];
    }
  }

  getUnreadCount(): number {
    return Array.from(this.conversations.values())
      .reduce((sum, conv) => sum + conv.unreadCount, 0);
  }

  clearConversationUnread(userId: string): void {
    const conversation = this.conversations.get(userId);
    if (conversation) {
      conversation.unreadCount = 0;
      this.conversationsUpdated$.next();
    }
  }

  setSelectedConversation(userId: string | null): void {
    this.selectedConversationUserId = userId || undefined;
  }

  markLoadedMessagesAsProcessed(messages: ChatMessage[]): void {
    messages.forEach(msg => {
      this.processedMessageIds.add(msg.id);
    });
  }

  private handleIncomingMessage(message: ChatMessage): void {
    // Skip if this message was already processed (deduplicate SignalR events)
    if (this.processedMessageIds.has(message.id)) {
      return;
    }
    this.processedMessageIds.add(message.id);

    if (!this.currentUserId) {
      return;
    }

    // Check if this is a message sent by current user
    const isSentByMe = message.senderId === this.currentUserId;

    // Find or create conversation with the OTHER user
    const otherUserId = isSentByMe ? message.receiverId : message.senderId;
    const otherUserName = isSentByMe ? 'Receiver' : message.senderName;

    let conversation = this.conversations.get(otherUserId);

    if (!conversation) {
      // Create new conversation for the other user
      conversation = {
        userId: otherUserId,
        userName: otherUserName,
        userEmail: '',
        unreadCount: 0,
        lastMessage: undefined
      };
      this.conversations.set(otherUserId, conversation);
    }

    // Update conversation
    conversation.lastMessage = message;

    // Increment unread count only if:
    // - Message is NOT sent by me
    // - AND this conversation is NOT currently selected
    if (!isSentByMe && this.selectedConversationUserId !== otherUserId) {
      conversation.unreadCount++;
    }

    // Emit conversations updated event
    this.conversationsUpdated$.next();
  }

  private handleMessageRead(senderId: string): void {
    const conversation = this.conversations.get(senderId);
    if (conversation && conversation.unreadCount > 0) {
      conversation.unreadCount = 0;
      this.conversationsUpdated$.next();
    }
  }
}
