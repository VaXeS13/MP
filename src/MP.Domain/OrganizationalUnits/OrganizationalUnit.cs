using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.OrganizationalUnits
{
    public class OrganizationalUnit : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public string Name { get; private set; } = null!;
        public string Code { get; private set; } = null!;
        public string? Address { get; private set; }
        public string? City { get; private set; }
        public string? PostalCode { get; private set; }
        public string? Email { get; private set; }
        public string? Phone { get; private set; }
        public bool IsActive { get; private set; }

        // EF Core constructor
        private OrganizationalUnit() { }

        public OrganizationalUnit(Guid id, string name, string code, Guid? tenantId = null)
            : base(id)
        {
            TenantId = tenantId;
            SetName(name);
            SetCode(code);
            IsActive = true;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("OrganizationalUnit.NameRequired", "Name is required");

            if (name.Length > 200)
                throw new BusinessException("OrganizationalUnit.NameTooLong", "Name cannot exceed 200 characters");

            Name = name;
        }

        public void SetCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("OrganizationalUnit.CodeRequired", "Code is required");

            if (code.Length > 50)
                throw new BusinessException("OrganizationalUnit.CodeTooLong", "Code cannot exceed 50 characters");

            if (!IsValidCode(code))
                throw new BusinessException("OrganizationalUnit.CodeInvalid", "Code can contain only alphanumeric characters and hyphens");

            Code = code;
        }

        public void UpdateContactInfo(string? address, string? city, string? postalCode, string? email, string? phone)
        {
            if (!string.IsNullOrWhiteSpace(address) && address.Length > 300)
                throw new BusinessException("OrganizationalUnit.AddressTooLong", "Address cannot exceed 300 characters");

            if (!string.IsNullOrWhiteSpace(city) && city.Length > 100)
                throw new BusinessException("OrganizationalUnit.CityTooLong", "City cannot exceed 100 characters");

            if (!string.IsNullOrWhiteSpace(postalCode) && postalCode.Length > 20)
                throw new BusinessException("OrganizationalUnit.PostalCodeTooLong", "Postal code cannot exceed 20 characters");

            if (!string.IsNullOrWhiteSpace(email))
            {
                if (email.Length > 255)
                    throw new BusinessException("OrganizationalUnit.EmailTooLong", "Email cannot exceed 255 characters");

                if (!IsValidEmail(email))
                    throw new BusinessException("OrganizationalUnit.EmailInvalid", "Invalid email format");
            }

            if (!string.IsNullOrWhiteSpace(phone) && phone.Length > 20)
                throw new BusinessException("OrganizationalUnit.PhoneTooLong", "Phone cannot exceed 20 characters");

            Address = address;
            City = city;
            PostalCode = postalCode;
            Email = email;
            Phone = phone;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        private static bool IsValidCode(string code)
        {
            return System.Text.RegularExpressions.Regex.IsMatch(code, @"^[a-zA-Z0-9-]+$");
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
