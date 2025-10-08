using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.Users;
using Volo.Abp.Authorization;
using MP.Application.Contracts.SignalR;
using MP.Permissions;
using Volo.Abp.Domain.Repositories;
using MP.Domain.Chat;
using System.Linq;

namespace MP.Hubs
{
    /// <summary>
    /// SignalR hub for real-time chat between admin and customers
    /// Direct communication channel for support and questions
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ICurrentUser _currentUser;
        private readonly IAuthorizationService _authorizationService;
        private readonly IRepository<ChatMessage, Guid> _chatMessageRepository;

        public ChatHub(
            ICurrentUser currentUser,
            IAuthorizationService authorizationService,
            IRepository<ChatMessage, Guid> chatMessageRepository)
        {
            _currentUser = currentUser;
            _authorizationService = authorizationService;
            _chatMessageRepository = chatMessageRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = _currentUser.Id;
            var tenantId = _currentUser.TenantId;

            if (userId.HasValue)
            {
                // Add to user's chat group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:user:{userId}");
            }

            // Check if user has permission to manage customer chats (admin/seller)
            var hasAdminPermission = await _authorizationService
                .IsGrantedAsync(MPPermissions.Chat.ManageCustomerChats);

            if (tenantId.HasValue && hasAdminPermission)
            {
                // Admins/sellers join tenant-wide chat group to receive all customer messages
                await Groups.AddToGroupAsync(Context.ConnectionId, $"chat:support:{tenantId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _currentUser.Id;
            var tenantId = _currentUser.TenantId;

            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:user:{userId}");
            }

            var hasAdminPermission = await _authorizationService
                .IsGrantedAsync(MPPermissions.Chat.ManageCustomerChats);

            if (tenantId.HasValue && hasAdminPermission)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"chat:support:{tenantId}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Send message to specific user
        /// </summary>
        public async Task SendMessage(Guid receiverId, string message)
        {
            var senderId = _currentUser.Id;
            var senderName = _currentUser.Name ?? _currentUser.UserName ?? "Unknown";
            var tenantId = _currentUser.TenantId;

            if (!senderId.HasValue)
            {
                return;
            }

            // Save message to database
            var chatMessageEntity = new ChatMessage(
                Guid.NewGuid(),
                senderId.Value,
                receiverId,
                message,
                tenantId
            );

            await _chatMessageRepository.InsertAsync(chatMessageEntity, autoSave: true);

            var chatMessage = new ChatMessageDto
            {
                Id = chatMessageEntity.Id,
                SenderId = chatMessageEntity.SenderId,
                SenderName = senderName,
                ReceiverId = chatMessageEntity.ReceiverId,
                Message = chatMessageEntity.Message,
                SentAt = chatMessageEntity.CreationTime,
                IsRead = chatMessageEntity.IsRead
            };

            // Send to specific receiver
            await Clients.Group($"chat:user:{receiverId}")
                .SendAsync("ReceiveMessage", chatMessage);

            // Also notify all support users if sender is customer
            var hasAdminPermission = await _authorizationService
                .IsGrantedAsync(MPPermissions.Chat.ManageCustomerChats);

            if (!hasAdminPermission && tenantId.HasValue)
            {
                // Customer sending message - notify all support staff
                await Clients.Group($"chat:support:{tenantId}")
                    .SendAsync("ReceiveMessage", chatMessage);
            }

            // Send confirmation to sender
            await Clients.Caller.SendAsync("MessageSent", chatMessage);
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        public async Task MarkMessageAsRead(Guid senderId)
        {
            var currentUserId = _currentUser.Id;
            if (!currentUserId.HasValue)
            {
                return;
            }

            // Find all unread messages from sender to current user
            var unreadMessages = await _chatMessageRepository.GetListAsync(
                m => m.SenderId == senderId &&
                     m.ReceiverId == currentUserId.Value &&
                     !m.IsRead
            );

            // Mark all as read in a single transaction
            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.MarkAsRead();
                    await _chatMessageRepository.UpdateAsync(message, autoSave: false);
                }

                // Save all changes in one transaction
                var dbContext = await _chatMessageRepository.GetDbContextAsync();
                await dbContext.SaveChangesAsync();

                // Notify the sender that their messages were read
                foreach (var message in unreadMessages)
                {
                    await Clients.Group($"chat:user:{senderId}")
                        .SendAsync("MessageRead", new {
                            messageId = message.Id,
                            readAt = message.ReadAt
                        });
                }
            }
        }

        /// <summary>
        /// Send typing indicator to specific user
        /// </summary>
        public async Task SendTypingIndicator(Guid receiverId, bool isTyping)
        {
            var currentUserId = _currentUser.Id;
            if (!currentUserId.HasValue)
            {
                return;
            }

            // Notify receiver that current user is typing
            await Clients.Group($"chat:user:{receiverId}")
                .SendAsync("UserTyping", new {
                    userId = currentUserId.Value,
                    isTyping
                });
        }
    }
}
