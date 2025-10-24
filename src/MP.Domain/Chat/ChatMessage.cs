using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Chat
{
    public class ChatMessage : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid? OrganizationalUnitId { get; private set; }
        public Guid SenderId { get; private set; }
        public Guid ReceiverId { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public bool IsRead { get; private set; }
        public DateTime? ReadAt { get; private set; }

        private ChatMessage() { }

        public ChatMessage(
            Guid id,
            Guid senderId,
            Guid receiverId,
            string message,
            Guid? organizationalUnitId = null,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SenderId = senderId;
            ReceiverId = receiverId;
            Message = message;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            if (!IsRead)
            {
                IsRead = true;
                ReadAt = DateTime.UtcNow;
            }
        }
    }
}
