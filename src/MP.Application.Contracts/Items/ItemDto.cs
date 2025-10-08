using System;
using Volo.Abp.Application.Dtos;

namespace MP.Items
{
    public class ItemDto : FullAuditedEntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = null!;
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
