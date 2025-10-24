using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Notifications
{
    /// <summary>
    /// User notification entity
    /// </summary>
    public class UserNotification : CreationAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid? OrganizationalUnitId { get; private set; }
        public Guid UserId { get; private set; }
        public string Type { get; private set; } = null!;
        public string Title { get; private set; } = null!;
        public string Message { get; private set; } = null!;
        public NotificationSeverity Severity { get; private set; }
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }
        public string? ActionUrl { get; private set; }
        public string? RelatedEntityType { get; private set; }
        public Guid? RelatedEntityId { get; private set; }
        public DateTime? ExpiresAt { get; private set; }

        private UserNotification() { }

        public UserNotification(
            Guid id,
            Guid userId,
            string type,
            string title,
            string message,
            NotificationSeverity severity = NotificationSeverity.Info,
            string? actionUrl = null,
            string? relatedEntityType = null,
            Guid? relatedEntityId = null,
            DateTime? expiresAt = null,
            Guid? organizationalUnitId = null,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            UserId = userId;
            SetType(type);
            SetTitle(title);
            SetMessage(message);
            Severity = severity;
            IsRead = false;
            ActionUrl = actionUrl?.Trim();
            RelatedEntityType = relatedEntityType?.Trim();
            RelatedEntityId = relatedEntityId;
            ExpiresAt = expiresAt;
        }

        public void SetType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new BusinessException("NOTIFICATION_TYPE_REQUIRED");

            Type = type.Trim();
        }

        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new BusinessException("NOTIFICATION_TITLE_REQUIRED");

            if (title.Length > 200)
                throw new BusinessException("NOTIFICATION_TITLE_TOO_LONG");

            Title = title.Trim();
        }

        public void SetMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new BusinessException("NOTIFICATION_MESSAGE_REQUIRED");

            if (message.Length > 2000)
                throw new BusinessException("NOTIFICATION_MESSAGE_TOO_LONG");

            Message = message.Trim();
        }

        public void MarkAsRead()
        {
            if (!IsRead)
            {
                IsRead = true;
                ReadAt = DateTime.Now;
            }
        }

        public void MarkAsUnread()
        {
            IsRead = false;
            ReadAt = null;
        }

        public bool IsExpired()
        {
            return ExpiresAt.HasValue && ExpiresAt.Value < DateTime.Now;
        }
    }

    /// <summary>
    /// Notification severity
    /// </summary>
    public enum NotificationSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Predefined notification types
    /// </summary>
    public static class NotificationTypes
    {
        public const string ItemSold = "ItemSold";
        public const string RentalExpiring = "RentalExpiring";
        public const string RentalExpired = "RentalExpired";
        public const string RentalExtended = "RentalExtended";
        public const string SettlementReady = "SettlementReady";
        public const string SettlementPaid = "SettlementPaid";
        public const string PaymentReceived = "PaymentReceived";
        public const string PaymentFailed = "PaymentFailed";
        public const string RentalStarted = "RentalStarted";
        public const string RentalCompleted = "RentalCompleted";
        public const string ItemExpiring = "ItemExpiring";
        public const string SystemAnnouncement = "SystemAnnouncement";
    }
}
