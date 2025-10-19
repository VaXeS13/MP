using System;
using System.Threading.Tasks;
using MP.Application.Contracts.Chat;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Chat
{
    public class ChatAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IChatAppService _chatAppService;

        public ChatAppServiceSimpleTests()
        {
            _chatAppService = GetRequiredService<IChatAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAvailableSupportUsersAsync_Should_Return_Support_Users()
        {
            // Act
            var result = await _chatAppService.GetAvailableSupportUsersAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMyConversationsAsync_Should_Return_Conversations()
        {
            // Act
            var result = await _chatAppService.GetMyConversationsAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetMessagesAsync_Should_Return_Messages()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();

            // Act
            var result = await _chatAppService.GetMessagesAsync(otherUserId);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task MarkMessagesAsReadAsync_Should_Mark_As_Read()
        {
            // Arrange
            var senderId = Guid.NewGuid();

            // Act & Assert
            await _chatAppService.MarkMessagesAsReadAsync(senderId);
        }
    }
}
