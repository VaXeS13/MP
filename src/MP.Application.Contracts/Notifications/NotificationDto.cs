using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace MP.Application.Contracts.Notifications
{
    /// <summary>
    /// Notification severity levels
    /// </summary>
    public enum NotificationSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }
    /// <summary>
    /// DTO for individual notification
    /// </summary>
    public class NotificationDto : EntityDto<Guid>
    {
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationSeverity Severity { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ActionUrl { get; set; }
        public string? RelatedEntityType { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// DTO for notification list response with pagination
    /// </summary>
    public class NotificationListDto : PagedResultDto<NotificationDto>
    {
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// Filter parameters for notification queries
    /// </summary>
    public class GetNotificationsInput : PagedAndSortedResultRequestDto
    {
        public bool? IsRead { get; set; }
        public NotificationSeverity? Severity { get; set; }
        public string? Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeExpired { get; set; } = false;
    }

    /// <summary>
    /// DTO for creating notification
    /// </summary>
    public class CreateNotificationDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = null!;

        public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;

        public string? ActionUrl { get; set; }
        public string? RelatedEntityType { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// DTO for notification statistics
    /// </summary>
    public class NotificationStatsDto
    {
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int ReadCount { get; set; }
        public int ExpiredCount { get; set; }
    }
}