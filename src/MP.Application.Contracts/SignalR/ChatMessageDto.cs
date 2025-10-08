using System;

namespace MP.Application.Contracts.SignalR
{
    /// <summary>
    /// DTO for real-time chat messages between admin and customer
    /// </summary>
    public class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderName { get; set; } = null!;
        public Guid ReceiverId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}
