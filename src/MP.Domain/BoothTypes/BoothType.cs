using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.BoothTypes
{
    public class BoothType : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }
        public string Name { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public decimal CommissionPercentage { get; private set; }
        public bool IsActive { get; private set; }

        // Konstruktor dla EF Core
        private BoothType() { }

        public BoothType(
            Guid id,
            string name,
            string description,
            decimal commissionPercentage,
            Guid organizationalUnitId,
            Guid? tenantId = null
        ) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SetName(name);
            SetDescription(description);
            SetCommissionPercentage(commissionPercentage);
            IsActive = true;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("BOOTH_TYPE_NAME_REQUIRED");

            if (name.Length > 100)
                throw new BusinessException("BOOTH_TYPE_NAME_TOO_LONG");

            Name = name.Trim();
        }

        public void SetDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new BusinessException("BOOTH_TYPE_DESCRIPTION_REQUIRED");

            if (description.Length > 500)
                throw new BusinessException("BOOTH_TYPE_DESCRIPTION_TOO_LONG");

            Description = description.Trim();
        }

        public void SetCommissionPercentage(decimal percentage)
        {
            if (percentage < 0 || percentage > 100)
                throw new BusinessException("INVALID_COMMISSION_PERCENTAGE")
                    .WithData("percentage", percentage);

            CommissionPercentage = percentage;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public decimal CalculateCommissionAmount(decimal salePrice)
        {
            return salePrice * (CommissionPercentage / 100);
        }
    }
}