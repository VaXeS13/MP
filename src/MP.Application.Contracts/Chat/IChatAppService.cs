using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace MP.Application.Contracts.Chat
{
    public interface IChatAppService : IApplicationService
    {
        /// <summary>
        /// Get available support users (admins/sellers) for customers to chat with
        /// </summary>
        Task<List<SupportUserDto>> GetAvailableSupportUsersAsync();

        /// <summary>
        /// Get all customers for support to start chat with (admin only)
        /// </summary>
        Task<List<CustomerUserDto>> GetAllCustomersAsync();

        /// <summary>
        /// Get all conversations for current user
        /// For customers: returns conversation with support
        /// For admins: returns all customer conversations
        /// </summary>
        Task<List<ChatConversationDto>> GetMyConversationsAsync();

        /// <summary>
        /// Get message history with specific user
        /// </summary>
        Task<List<ChatMessageDto>> GetMessagesAsync(Guid otherUserId);

        /// <summary>
        /// Mark messages as read
        /// </summary>
        Task MarkMessagesAsReadAsync(Guid senderId);
    }
}
