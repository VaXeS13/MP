using System;
using Volo.Abp.Application.Dtos;

namespace MP.Payments
{
    public class GetPaymentTransactionListDto : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public Guid? RentalId { get; set; }
        public string? Email { get; set; }
    }
}