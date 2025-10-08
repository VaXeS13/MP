using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace MP.Rentals
{
    public class GetRentalListDto : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; }
        public RentalStatus? Status { get; set; }
        public Guid? UserId { get; set; }
        public Guid? BoothId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsOverdue { get; set; }
    }
}