using System;
using Volo.Abp.Application.Dtos;
using MP.Domain.FloorPlans;

namespace MP.FloorPlans
{
    public class FloorPlanElementDto : FullAuditedEntityDto<Guid>
    {
        public Guid FloorPlanId { get; set; }
        public FloorPlanElementType ElementType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Rotation { get; set; }
        public string? Color { get; set; }
        public string? Text { get; set; }
        public string? IconName { get; set; }
        public int? Thickness { get; set; }
        public double? Opacity { get; set; }
        public string? Direction { get; set; }
    }
}
