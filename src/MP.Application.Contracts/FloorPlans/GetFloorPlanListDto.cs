using System;
using Volo.Abp.Application.Dtos;

namespace MP.FloorPlans
{
    public class GetFloorPlanListDto : PagedAndSortedResultRequestDto
    {
        public Guid? TenantId { get; set; }
        public bool? IsActive { get; set; }
        public int? Level { get; set; }
        public string? Filter { get; set; }
    }
}