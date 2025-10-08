using System;

namespace MP.Application.Contracts.SignalR
{
    /// <summary>
    /// DTO for SignalR notification messages
    /// </summary>
    public class NotificationMessageDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Severity { get; set; } = "info";
        public string? ActionUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
