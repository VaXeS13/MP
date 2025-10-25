using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MP.OrganizationalUnits.Dtos
{
    public class UpdateUnitSettingsDto
    {
        [Required(ErrorMessage = "Currency is required")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be 3 characters")]
        public string Currency { get; set; } = "PLN";

        public Dictionary<string, bool>? EnabledPaymentProviders { get; set; }

        [StringLength(50, ErrorMessage = "Default payment provider cannot exceed 50 characters")]
        public string? DefaultPaymentProvider { get; set; }

        [StringLength(500, ErrorMessage = "Logo URL cannot exceed 500 characters")]
        [Url(ErrorMessage = "Invalid URL format")]
        public string? LogoUrl { get; set; }

        [StringLength(1000, ErrorMessage = "Banner text cannot exceed 1000 characters")]
        public string? BannerText { get; set; }
    }
}
