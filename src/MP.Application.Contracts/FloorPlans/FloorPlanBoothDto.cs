using System;
using Volo.Abp.Application.Dtos;
using MP.Booths;

namespace MP.FloorPlans
{
    public class FloorPlanBoothDto : FullAuditedEntityDto<Guid>
    {
        public Guid FloorPlanId { get; set; }
        public Guid BoothId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Rotation { get; set; }
        public BoothDto? Booth { get; set; }
    }
}