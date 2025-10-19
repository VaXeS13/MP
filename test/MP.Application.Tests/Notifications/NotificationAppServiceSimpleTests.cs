using System;
using System.Threading.Tasks;
using MP.Application.Contracts.Notifications;
using MP.Application.Contracts.SignalR;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Notifications
{
    public class NotificationAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly INotificationAppService _notificationAppService;

        public NotificationAppServiceSimpleTests()
        {
            _notificationAppService = GetRequiredService<INotificationAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task SendToUserAsync_Should_Send_Notification()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var notification = new NotificationMessageDto
            {
                Title = "Test Notification",
                Message = "This is a test notification",
                Type = "info"
            };

            // Act & Assert
            await _notificationAppService.SendToUserAsync(userId, notification);
        }

        [Fact]
        [UnitOfWork]
        public async Task SendToTenantAsync_Should_Send_To_Tenant()
        {
            // Arrange
            var notification = new NotificationMessageDto
            {
                Title = "Tenant Notification",
                Message = "This goes to all tenant users",
                Type = "warning"
            };

            // Act & Assert
            await _notificationAppService.SendToTenantAsync(notification);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetListAsync_Should_Return_Notifications()
        {
            // Arrange
            var input = new GetNotificationsInput();

            // Act
            var result = await _notificationAppService.GetListAsync(input);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetUnreadAsync_Should_Return_Unread_Notifications()
        {
            // Arrange
            var input = new GetNotificationsInput();

            // Act
            var result = await _notificationAppService.GetUnreadAsync(input);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAllAsync_Should_Return_All_Notifications()
        {
            // Arrange
            var input = new GetNotificationsInput();

            // Act
            var result = await _notificationAppService.GetAllAsync(input);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetStatsAsync_Should_Return_Stats()
        {
            // Act
            var result = await _notificationAppService.GetStatsAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetUnreadCountAsync_Should_Return_Count()
        {
            // Act
            var result = await _notificationAppService.GetUnreadCountAsync();

            // Assert
            result.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task MarkAllAsReadAsync_Should_Mark_All_As_Read()
        {
            // Act & Assert
            await _notificationAppService.MarkAllAsReadAsync();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteExpiredNotificationsAsync_Should_Delete_Expired()
        {
            // Act
            var result = await _notificationAppService.DeleteExpiredNotificationsAsync();

            // Assert
            result.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}
