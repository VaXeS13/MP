using MP.Domain.Booths;
using System;
using Volo.Abp.Application.Dtos;

namespace MP.Booths;

public class BoothDto : FullAuditedEntityDto<Guid>
{
    public string Number { get; set; } = null!;
    public BoothStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = null!;
    public decimal PricePerDay { get; set; }
    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }

    // Current active rental information
    public Guid? CurrentRentalId { get; set; }
    public string? CurrentRentalUserName { get; set; }
    public string? CurrentRentalUserEmail { get; set; }
    public DateTime? CurrentRentalStartDate { get; set; }
    public DateTime? CurrentRentalEndDate { get; set; }
}
