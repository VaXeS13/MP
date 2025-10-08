using System;
using Volo.Abp.Application.Dtos;
using MP.Domain.FloorPlans;

namespace MP.FloorPlans
{
    public class GetFloorPlanElementListDto : PagedAndSortedResultRequestDto
    {
        public Guid? FloorPlanId { get; set; }
        public FloorPlanElementType? ElementType { get; set; }
    }
}
