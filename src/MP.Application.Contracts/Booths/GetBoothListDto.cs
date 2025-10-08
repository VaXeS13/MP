using MP.Domain.Booths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace MP.Booths
{
    public class GetBoothListDto : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; }
        public BoothStatus? Status { get; set; }
    }
}
