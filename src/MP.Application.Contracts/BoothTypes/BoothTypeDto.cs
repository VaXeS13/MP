using System;
using Volo.Abp.Application.Dtos;

namespace MP.Application.Contracts.BoothTypes
{
    public class BoothTypeDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal CommissionPercentage { get; set; }
        public bool IsActive { get; set; }
    }
}