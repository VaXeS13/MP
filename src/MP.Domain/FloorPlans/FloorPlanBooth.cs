using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using MP.Domain.Booths;

namespace MP.Domain.FloorPlans
{
    public class FloorPlanBooth : FullAuditedEntity<Guid>
    {
        public Guid FloorPlanId { get; private set; }
        public Guid BoothId { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Rotation { get; private set; }

        // Navigation properties
        public virtual Booth Booth { get; set; } = null!;

        private FloorPlanBooth() { }

        public FloorPlanBooth(
            Guid id,
            Guid floorPlanId,
            Guid boothId,
            int x,
            int y,
            int width,
            int height,
            int rotation = 0
        ) : base(id)
        {
            FloorPlanId = floorPlanId;
            BoothId = boothId;
            SetPosition(x, y);
            SetDimensions(width, height);
            SetRotation(rotation);
        }

        public void SetPosition(int x, int y)
        {
            if (x < 0 || y < 0)
                throw new BusinessException("FLOOR_PLAN_BOOTH_POSITION_INVALID");

            X = x;
            Y = y;
        }

        public void SetDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new BusinessException("FLOOR_PLAN_BOOTH_DIMENSIONS_INVALID");

            if (width > 1000 || height > 1000)
                throw new BusinessException("FLOOR_PLAN_BOOTH_DIMENSIONS_TOO_LARGE");

            Width = width;
            Height = height;
        }

        public void SetRotation(int rotation)
        {
            if (rotation < 0 || rotation >= 360)
                throw new BusinessException("FLOOR_PLAN_BOOTH_ROTATION_INVALID");

            Rotation = rotation;
        }

        public void UpdatePosition(int x, int y, int width, int height, int rotation = 0)
        {
            SetPosition(x, y);
            SetDimensions(width, height);
            SetRotation(rotation);
        }
    }
}