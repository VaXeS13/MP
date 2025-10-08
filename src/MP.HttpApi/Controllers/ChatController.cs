using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using MP.Application.Contracts.Chat;

namespace MP.Controllers
{
    [RemoteService(Name = "Default")]
    [Area("app")]
    [Route("api/app/chat")]
    public class ChatController : AbpControllerBase
    {
        private readonly IChatAppService _chatAppService;

        public ChatController(IChatAppService chatAppService)
        {
            _chatAppService = chatAppService;
        }

        [HttpGet("support-users")]
        public Task<List<SupportUserDto>> GetAvailableSupportUsersAsync()
        {
            return _chatAppService.GetAvailableSupportUsersAsync();
        }

        [HttpGet("conversations")]
        public Task<List<ChatConversationDto>> GetMyConversationsAsync()
        {
            return _chatAppService.GetMyConversationsAsync();
        }

        [HttpGet("messages")]
        public Task<List<ChatMessageDto>> GetMessagesAsync(Guid otherUserId)
        {
            return _chatAppService.GetMessagesAsync(otherUserId);
        }

        [HttpPost("mark-read")]
        public Task MarkMessagesAsReadAsync(Guid senderId)
        {
            return _chatAppService.MarkMessagesAsReadAsync(senderId);
        }

        [HttpGet("customers")]
        public Task<List<CustomerUserDto>> GetAllCustomersAsync()
        {
            return _chatAppService.GetAllCustomersAsync();
        }
    }
}
