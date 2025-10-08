using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MP.FloorPlans
{
    public class FloorPlanDto : FullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = null!;
        public int Level { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsActive { get; set; }
        public List<FloorPlanBoothDto> Booths { get; set; } = new();
        public List<FloorPlanElementDto> Elements { get; set; } = new();
    }
}