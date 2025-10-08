using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace MP.Rentals
{
    public class RentalListDto : EntityDto<Guid>
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;

        public Guid BoothId { get; set; }
        public string BoothNumber { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysCount { get; set; }

        public RentalStatus Status { get; set; }
        public string StatusDisplayName { get; set; } = null!;

        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsPaid { get; set; }

        public DateTime CreationTime { get; set; }
        public DateTime? StartedAt { get; set; }

        // Statystyki
        public int ItemsCount { get; set; }
        public int SoldItemsCount { get; set; }
    }
}
