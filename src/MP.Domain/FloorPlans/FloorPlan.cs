using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.FloorPlans
{
    public class FloorPlan : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }
        public Guid OrganizationalUnitId { get; private set; }
        public string Name { get; private set; } = null!;
        public int Level { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool IsActive { get; private set; }

        private FloorPlan() { }

        public FloorPlan(
            Guid id,
            string name,
            int level,
            int width,
            int height,
            Guid organizationalUnitId,
            Guid? tenantId = null
        ) : base(id)
        {
            TenantId = tenantId;
            OrganizationalUnitId = organizationalUnitId;
            SetName(name);
            SetLevel(level);
            SetDimensions(width, height);
            IsActive = false;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BusinessException("FLOOR_PLAN_NAME_REQUIRED");

            if (name.Length > 100)
                throw new BusinessException("FLOOR_PLAN_NAME_TOO_LONG");

            Name = name.Trim();
        }

        public void SetLevel(int level)
        {
            if (level < 0)
                throw new BusinessException("FLOOR_PLAN_LEVEL_INVALID");

            Level = level;
        }

        public void SetDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new BusinessException("FLOOR_PLAN_DIMENSIONS_INVALID");

            if (width > 10000 || height > 10000)
                throw new BusinessException("FLOOR_PLAN_DIMENSIONS_TOO_LARGE");

            Width = width;
            Height = height;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Publish()
        {
            IsActive = true;
        }
    }
}