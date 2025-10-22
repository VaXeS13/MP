using MP.Domain.Booths;
using MP.Application.Contracts.Booths;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace MP.Booths;

public class BoothDto : FullAuditedEntityDto<Guid>
{
    public string Number { get; set; } = null!;
    public BoothStatus Status { get; set; }
    public string StatusDisplayName { get; set; } = null!;

    /// <summary>
    /// Legacy price per day - kept for backward compatibility
    /// Use PricingPeriods for new multi-period pricing
    /// </summary>
    [Obsolete("Use PricingPeriods instead. This property is kept for backward compatibility.")]
    public decimal PricePerDay { get; set; }

    /// <summary>
    /// Multi-period pricing configuration
    /// Example: [{ Days: 1, PricePerPeriod: 5 }, { Days: 7, PricePerPeriod: 30 }]
    /// </summary>
    public List<BoothPricingPeriodDto> PricingPeriods { get; set; } = new();

    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }

    // Current active rental information
    public Guid? CurrentRentalId { get; set; }
    public string? CurrentRentalUserName { get; set; }
    public string? CurrentRentalUserEmail { get; set; }
    public DateTime? CurrentRentalStartDate { get; set; }
    public DateTime? CurrentRentalEndDate { get; set; }
}
