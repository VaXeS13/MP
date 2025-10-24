using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.OrganizationalUnits
{
    /// <summary>
    /// Represents an organizational unit (physical location/branch) within a tenant.
    /// Each tenant can have multiple organizational units with independent booth management,
    /// user assignments, and operational settings.
    /// </summary>
    public class OrganizationalUnit : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        /// <summary>
        /// The tenant ID this organizational unit belongs to.
        /// </summary>
        public Guid? TenantId { get; private set; }

        /// <summary>
        /// Display name of the organizational unit (max 200 characters).
        /// Example: "Główna", "Centrum Warszawy", "Filia Poznańska"
        /// </summary>
        public string Name { get; private set; } = null!;

        /// <summary>
        /// Unique code within the tenant (max 50 characters).
        /// Used in registration codes and as identifiable short name.
        /// Example: "MAIN", "WARSAW-CENTER", "POZNAN-WEST"
        /// </summary>
        public string Code { get; private set; } = null!;

        /// <summary>
        /// Street address of the organizational unit.
        /// </summary>
        public string? Address { get; private set; }

        /// <summary>
        /// City where this organizational unit is located.
        /// </summary>
        public string? City { get; private set; }

        /// <summary>
        /// Postal code for the organizational unit's address.
        /// </summary>
        public string? PostalCode { get; private set; }

        /// <summary>
        /// Contact email for the organizational unit.
        /// </summary>
        public string? Email { get; private set; }

        /// <summary>
        /// Contact phone number for the organizational unit.
        /// </summary>
        public string? Phone { get; private set; }

        /// <summary>
        /// Whether this organizational unit is active.
        /// Inactive units cannot accept new bookings or rentals.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private OrganizationalUnit()
        {
        }

        /// <summary>
        /// Creates a new organizational unit.
        /// </summary>
        /// <param name="id">Unique identifier for the unit.</param>
        /// <param name="name">Display name (required, max 200 chars).</param>
        /// <param name="code">Unique code within tenant (required, max 50 chars).</param>
        /// <param name="tenantId">Optional tenant ID for multi-tenancy.</param>
        public OrganizationalUnit(
            Guid id,
            string name,
            string code,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            SetName(name);
            SetCode(code);
            IsActive = true;
        }

        /// <summary>
        /// Sets the name of the organizational unit with validation.
        /// </summary>
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("ORGANIZATIONAL_UNIT_NAME_REQUIRED");

            if (name.Length > 200)
                throw new BusinessException("ORGANIZATIONAL_UNIT_NAME_TOO_LONG")
                    .WithData("maxLength", 200)
                    .WithData("providedLength", name.Length);

            Name = name.Trim();
        }

        /// <summary>
        /// Sets the code of the organizational unit with validation.
        /// Code must be alphanumeric with hyphens, unique per tenant.
        /// </summary>
        public void SetCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("ORGANIZATIONAL_UNIT_CODE_REQUIRED");

            if (code.Length > 50)
                throw new BusinessException("ORGANIZATIONAL_UNIT_CODE_TOO_LONG")
                    .WithData("maxLength", 50)
                    .WithData("providedLength", code.Length);

            // Validate code format: alphanumeric and hyphens only
            if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z0-9\-]+$"))
                throw new BusinessException("ORGANIZATIONAL_UNIT_CODE_INVALID_FORMAT");

            Code = code.Trim().ToUpper();
        }

        /// <summary>
        /// Updates the contact information for this organizational unit.
        /// </summary>
        public void UpdateContactInfo(
            string? address,
            string? city,
            string? postalCode,
            string? email,
            string? phone)
        {
            Address = address?.Trim();
            City = city?.Trim();
            PostalCode = postalCode?.Trim();

            if (!string.IsNullOrWhiteSpace(email))
            {
                // Basic email validation
                if (!System.ComponentModel.DataAnnotations.Validator.TryValidateValue(
                    email,
                    new System.ComponentModel.DataAnnotations.ValidationContext(this),
                    null,
                    new[] { new System.ComponentModel.DataAnnotations.EmailAddressAttribute() }))
                {
                    throw new BusinessException("ORGANIZATIONAL_UNIT_EMAIL_INVALID_FORMAT");
                }
                Email = email.Trim().ToLower();
            }
            else
            {
                Email = null;
            }

            Phone = phone?.Trim();
        }

        /// <summary>
        /// Activates this organizational unit, allowing it to accept bookings.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
        }

        /// <summary>
        /// Deactivates this organizational unit.
        /// Inactive units cannot accept new bookings or rentals.
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
