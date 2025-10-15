using System.ComponentModel.DataAnnotations;
using System;
using Volo.Abp.Domain.Entities;

namespace MP.Domain.Identity;

public class UserProfile : Entity<Guid>
{
    public Guid UserId { get; set; }

    [MaxLength(50)]
    public string BankAccountNumber { get; set; }

    // Navigation property
    public virtual Volo.Abp.Identity.IdentityUser User { get; set; }
}