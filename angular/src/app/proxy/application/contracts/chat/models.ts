
export interface ChatConversationDto {
  userId?: string;
  userName?: string;
  userEmail?: string;
  unreadCount: number;
  lastMessage: ChatMessageDto;
}

export interface ChatMessageDto {
  id?: string;
  senderId?: string;
  senderName?: string;
  receiverId?: string;
  message?: string;
  sentAt?: string;
  isRead: boolean;
}

export interface CustomerUserDto {
  id?: string;
  name?: string;
  email?: string;
  isOnline: boolean;
}

export interface SupportUserDto {
  id?: string;
  name?: string;
  email?: string;
  isOnline: boolean;
}
