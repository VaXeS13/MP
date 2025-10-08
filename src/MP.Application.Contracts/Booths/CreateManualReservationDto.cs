using MP.Domain.Booths;
using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Booths;

public class CreateManualReservationDto
{
    [Required]
    public Guid BoothId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public BoothStatus TargetStatus { get; set; } // Reserved or Rented
}
