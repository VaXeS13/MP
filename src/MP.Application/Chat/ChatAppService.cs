using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using MP.Application.Contracts.Chat;
using MP.Permissions;
using Volo.Abp.Domain.Repositories;
using MP.Domain.Chat;

namespace MP.Application.Chat
{
    [Authorize]
    public class ChatAppService : ApplicationService, IChatAppService
    {
        private readonly IIdentityUserRepository _userRepository;
        private readonly IdentityUserManager _userManager;
        private readonly IRepository<ChatMessage, Guid> _chatMessageRepository;

        public ChatAppService(
            IIdentityUserRepository userRepository,
            IdentityUserManager userManager,
            IRepository<ChatMessage, Guid> chatMessageRepository)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _chatMessageRepository = chatMessageRepository;
        }

        public async Task<List<SupportUserDto>> GetAvailableSupportUsersAsync()
        {
            // Get all users with Chat.ManageCustomerChats permission (admins/sellers)
            var allUsers = await _userRepository.GetListAsync();
            var supportUsers = new List<SupportUserDto>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Check if user has admin role or specific chat management permission
                if (roles.Contains("admin") || await _userManager.IsInRoleAsync(user, "admin"))
                {
                    supportUsers.Add(new SupportUserDto
                    {
                        Id = user.Id,
                        Name = user.Name ?? user.UserName ?? "Support",
                        Email = user.Email ?? "",
                        IsOnline = false // TODO: Implement online status tracking
                    });
                }
            }

            return supportUsers;
        }

        public async Task<List<CustomerUserDto>> GetAllCustomersAsync()
        {
            // Check if current user has admin permission
            var hasAdminPermission = await AuthorizationService
                .IsGrantedAsync(MPPermissions.Chat.ManageCustomerChats);

            if (!hasAdminPermission)
            {
                throw new Volo.Abp.Authorization.AbpAuthorizationException("You don't have permission to access customer list");
            }

            // Get all users
            var allUsers = await _userRepository.GetListAsync();
            var customers = new List<CustomerUserDto>();

            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Get users who are NOT admins (regular customers)
                if (!roles.Contains("admin") && !await _userManager.IsInRoleAsync(user, "admin"))
                {
                    customers.Add(new CustomerUserDto
                    {
                        Id = user.Id,
                        Name = user.Name ?? user.UserName ?? "Customer",
                        Email = user.Email ?? "",
                        IsOnline = false // TODO: Implement online status tracking
                    });
                }
            }

            return customers.OrderBy(c => c.Name).ToList();
        }

        public async Task<List<ChatConversationDto>> GetMyConversationsAsync()
        {
            var currentUserId = CurrentUser.GetId();
            var hasAdminPermission = await AuthorizationService
                .IsGrantedAsync(MPPermissions.Chat.ManageCustomerChats);

            var conversations = new List<ChatConversationDto>();

            if (hasAdminPermission)
            {
                // Admin view: Get all users who have sent or received messages from this admin
                var messages = await _chatMessageRepository.GetListAsync(
                    m => m.SenderId == currentUserId || m.ReceiverId == currentUserId
                );

                // Group by other user ID
                var otherUserIds = messages
                    .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToList();

                if (otherUserIds.Any())
                {
                    var users = await _userRepository.GetListAsync();
                    var userDict = users.Where(u => otherUserIds.Contains(u.Id))
                        .ToDictionary(u => u.Id, u => u);

                    foreach (var otherUserId in otherUserIds)
                    {
                        // Get last message with this user
                        var lastMessage = messages
                            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                       (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                            .OrderByDescending(m => m.CreationTime)
                            .FirstOrDefault();

                        // Count unread messages from this user to current admin
                        var unreadCount = messages
                            .Count(m => m.SenderId == otherUserId &&
                                       m.ReceiverId == currentUserId &&
                                       !m.IsRead);

                        if (userDict.TryGetValue(otherUserId, out var user))
                        {
                            conversations.Add(new ChatConversationDto
                            {
                                UserId = user.Id,
                                UserName = user.Name ?? user.UserName ?? "Unknown",
                                UserEmail = user.Email ?? "",
                                UnreadCount = unreadCount,
                                LastMessage = lastMessage != null ? new ChatMessageDto
                                {
                                    Id = lastMessage.Id,
                                    SenderId = lastMessage.SenderId,
                                    SenderName = userDict.ContainsKey(lastMessage.SenderId) ?
                                        (userDict[lastMessage.SenderId].Name ?? userDict[lastMessage.SenderId].UserName ?? "Unknown") :
                                        "Unknown",
                                    ReceiverId = lastMessage.ReceiverId,
                                    Message = lastMessage.Message,
                                    SentAt = lastMessage.CreationTime,
                                    IsRead = lastMessage.IsRead
                                } : null
                            });
                        }
                    }

                    // Sort by last message time descending
                    conversations = conversations
                        .OrderByDescending(c => c.LastMessage?.SentAt ?? DateTime.MinValue)
                        .ToList();
                }

                return conversations;
            }
            else
            {
                // Customer view: Get all messages involving current user
                var messages = await _chatMessageRepository.GetListAsync(
                    m => m.SenderId == currentUserId || m.ReceiverId == currentUserId
                );

                if (messages.Any())
                {
                    // Group by other user ID (support users)
                    var otherUserIds = messages
                        .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                        .Distinct()
                        .ToList();

                    var users = await _userRepository.GetListAsync();
                    var userDict = users.Where(u => otherUserIds.Contains(u.Id))
                        .ToDictionary(u => u.Id, u => u);

                    foreach (var otherUserId in otherUserIds)
                    {
                        // Get last message with this user
                        var lastMessage = messages
                            .Where(m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                                       (m.SenderId == otherUserId && m.ReceiverId == currentUserId))
                            .OrderByDescending(m => m.CreationTime)
                            .FirstOrDefault();

                        // Count unread messages from support user to current customer
                        var unreadCount = messages
                            .Count(m => m.SenderId == otherUserId &&
                                       m.ReceiverId == currentUserId &&
                                       !m.IsRead);

                        if (userDict.TryGetValue(otherUserId, out var user))
                        {
                            conversations.Add(new ChatConversationDto
                            {
                                UserId = user.Id,
                                UserName = user.Name ?? user.UserName ?? "Support",
                                UserEmail = user.Email ?? "",
                                UnreadCount = unreadCount,
                                LastMessage = lastMessage != null ? new ChatMessageDto
                                {
                                    Id = lastMessage.Id,
                                    SenderId = lastMessage.SenderId,
                                    SenderName = userDict.ContainsKey(lastMessage.SenderId) ?
                                        (userDict[lastMessage.SenderId].Name ?? userDict[lastMessage.SenderId].UserName ?? "Unknown") :
                                        "Unknown",
                                    ReceiverId = lastMessage.ReceiverId,
                                    Message = lastMessage.Message,
                                    SentAt = lastMessage.CreationTime,
                                    IsRead = lastMessage.IsRead
                                } : null
                            });
                        }
                    }

                    // Sort by last message time descending
                    conversations = conversations
                        .OrderByDescending(c => c.LastMessage?.SentAt ?? DateTime.MinValue)
                        .ToList();
                }
                else
                {
                    // No messages yet - show available support users
                    var supportUsers = await GetAvailableSupportUsersAsync();

                    foreach (var supportUser in supportUsers)
                    {
                        conversations.Add(new ChatConversationDto
                        {
                            UserId = supportUser.Id,
                            UserName = supportUser.Name,
                            UserEmail = supportUser.Email,
                            UnreadCount = 0,
                            LastMessage = null
                        });
                    }
                }

                return conversations;
            }
        }

        public async Task<List<ChatMessageDto>> GetMessagesAsync(Guid otherUserId)
        {
            var currentUserId = CurrentUser.GetId();

            // Get all messages between current user and other user
            var messages = await _chatMessageRepository.GetListAsync(
                m => (m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                     (m.SenderId == otherUserId && m.ReceiverId == currentUserId)
            );

            // Get user info for sender names
            var userIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var allUsers = await _userRepository.GetListAsync();
            var users = allUsers.Where(u => userIds.Contains(u.Id)).ToList();
            var userDict = users.ToDictionary(u => u.Id, u => u.Name ?? u.UserName ?? "Unknown");

            // Sort by creation time and map to DTOs
            return messages
                .OrderBy(m => m.CreationTime)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = userDict.ContainsKey(m.SenderId) ? userDict[m.SenderId] : "Unknown",
                    ReceiverId = m.ReceiverId,
                    Message = m.Message,
                    SentAt = m.CreationTime,
                    IsRead = m.IsRead
                })
                .ToList();
        }

        public async Task MarkMessagesAsReadAsync(Guid senderId)
        {
            var currentUserId = CurrentUser.GetId();

            // Find all unread messages from sender to current user
            var unreadMessages = await _chatMessageRepository.GetListAsync(
                m => m.SenderId == senderId &&
                     m.ReceiverId == currentUserId &&
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
            }
        }
    }
}
