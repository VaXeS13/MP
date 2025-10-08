import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { ChatConversationDto, ChatMessageDto, CustomerUserDto, SupportUserDto } from '../application/contracts/chat/models';

@Injectable({
  providedIn: 'root',
})
export class ChatService {
  apiName = 'Default';
  

  getAllCustomers = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, CustomerUserDto[]>({
      method: 'GET',
      url: '/api/app/chat/customers',
    },
    { apiName: this.apiName,...config });
  

  getAvailableSupportUsers = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, SupportUserDto[]>({
      method: 'GET',
      url: '/api/app/chat/support-users',
    },
    { apiName: this.apiName,...config });
  

  getMessages = (otherUserId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ChatMessageDto[]>({
      method: 'GET',
      url: '/api/app/chat/messages',
      params: { otherUserId },
    },
    { apiName: this.apiName,...config });
  

  getMyConversations = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ChatConversationDto[]>({
      method: 'GET',
      url: '/api/app/chat/conversations',
    },
    { apiName: this.apiName,...config });
  

  markMessagesAsRead = (senderId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: '/api/app/chat/mark-read',
      params: { senderId },
    },
    { apiName: this.apiName,...config });

  constructor(private restService: RestService) {}
}
