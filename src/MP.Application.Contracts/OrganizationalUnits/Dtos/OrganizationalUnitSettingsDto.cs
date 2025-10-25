using System;
using System.Collections.Generic;

namespace MP.OrganizationalUnits.Dtos
{
    public class OrganizationalUnitSettingsDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationalUnitId { get; set; }
        public string Currency { get; set; } = "PLN";
        public Dictionary<string, bool> EnabledPaymentProviders { get; set; } = new();
        public string? DefaultPaymentProvider { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerText { get; set; }
        public bool IsMainUnit { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime? LastModificationTime { get; set; }
    }
}
