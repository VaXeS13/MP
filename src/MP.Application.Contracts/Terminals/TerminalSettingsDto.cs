using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Application.Contracts.Terminals
{
    public class TerminalSettingsDto
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public string ProviderId { get; set; } = null!;
        public bool IsEnabled { get; set; }
        public string ConfigurationJson { get; set; } = "{}";
        public string Currency { get; set; } = "PLN";
        public string? Region { get; set; }
        public bool IsSandbox { get; set; }
    }

    public class CreateTerminalSettingsDto
    {
        [Required]
        [StringLength(50)]
        public string ProviderId { get; set; } = null!;

        public bool IsEnabled { get; set; } = true;

        [StringLength(4000)]
        public string ConfigurationJson { get; set; } = "{}";

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "PLN";

        [StringLength(10)]
        public string? Region { get; set; }

        public bool IsSandbox { get; set; }
    }

    public class UpdateTerminalSettingsDto
    {
        [Required]
        [StringLength(50)]
        public string ProviderId { get; set; } = null!;

        public bool IsEnabled { get; set; }

        [StringLength(4000)]
        public string ConfigurationJson { get; set; } = "{}";

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "PLN";

        [StringLength(10)]
        public string? Region { get; set; }

        public bool IsSandbox { get; set; }
    }

    public class TerminalProviderInfoDto
    {
        public string ProviderId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}