using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace MP.Domain.FloorPlans
{
    public class FloorPlanElement : FullAuditedEntity<Guid>
    {
        public Guid FloorPlanId { get; private set; }
        public FloorPlanElementType ElementType { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Rotation { get; private set; }

        // Optional properties for specific element types
        public string? Color { get; private set; }
        public string? Text { get; private set; }
        public string? IconName { get; private set; }
        public int? Thickness { get; private set; }
        public double? Opacity { get; private set; }
        public string? Direction { get; private set; } // For doors: left, right, up, down

        private FloorPlanElement() { }

        public FloorPlanElement(
            Guid id,
            Guid floorPlanId,
            FloorPlanElementType elementType,
            int x,
            int y,
            int width,
            int height,
            int rotation = 0,
            string? color = null,
            string? text = null,
            string? iconName = null,
            int? thickness = null,
            double? opacity = null,
            string? direction = null
        ) : base(id)
        {
            FloorPlanId = floorPlanId;
            ElementType = elementType;
            SetPosition(x, y);
            SetDimensions(width, height);
            SetRotation(rotation);
            SetColor(color);
            SetText(text);
            SetIconName(iconName);
            SetThickness(thickness);
            SetOpacity(opacity);
            SetDirection(direction);
        }

        public void SetPosition(int x, int y)
        {
            if (x < 0 || y < 0)
                throw new BusinessException("FLOOR_PLAN_ELEMENT_POSITION_INVALID");

            X = x;
            Y = y;
        }

        public void SetDimensions(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new BusinessException("FLOOR_PLAN_ELEMENT_DIMENSIONS_INVALID");

            if (width > 5000 || height > 5000)
                throw new BusinessException("FLOOR_PLAN_ELEMENT_DIMENSIONS_TOO_LARGE");

            Width = width;
            Height = height;
        }

        public void SetRotation(int rotation)
        {
            if (rotation < 0 || rotation >= 360)
                throw new BusinessException("FLOOR_PLAN_ELEMENT_ROTATION_INVALID");

            Rotation = rotation;
        }

        public void SetColor(string? color)
        {
            if (!string.IsNullOrWhiteSpace(color) && color.Length > 20)
                throw new BusinessException("FLOOR_PLAN_ELEMENT_COLOR_TOO_LONG");

            Color = color;
        }

        public void SetText(string? text)
        {
            if (!string.IsNullOrWhiteSpace(text) && text.Length > 500)
                throw new BusinessException("FLOOR_PLAN_ELEMENT_TEXT_TOO_LONG");

            Text = text;
        }

        public void SetIconName(string? iconName)
        {
            if (!string.IsNullOrWhiteSpace(iconName) && iconName.Length > 50)
                throw new BusinessException("FLOOR_PLAN_ELEMENT_ICON_NAME_TOO_LONG");

            IconName = iconName;
        }

        public void SetThickness(int? thickness)
        {
            if (thickness.HasValue && (thickness.Value <= 0 || thickness.Value > 100))
                throw new BusinessException("FLOOR_PLAN_ELEMENT_THICKNESS_INVALID");

            Thickness = thickness;
        }

        public void SetOpacity(double? opacity)
        {
            if (opacity.HasValue && (opacity.Value < 0 || opacity.Value > 1))
                throw new BusinessException("FLOOR_PLAN_ELEMENT_OPACITY_INVALID");

            Opacity = opacity;
        }

        public void SetDirection(string? direction)
        {
            if (!string.IsNullOrWhiteSpace(direction))
            {
                var validDirections = new[] { "left", "right", "up", "down" };
                if (!Array.Exists(validDirections, d => d.Equals(direction, StringComparison.OrdinalIgnoreCase)))
                    throw new BusinessException("FLOOR_PLAN_ELEMENT_DIRECTION_INVALID");
            }

            Direction = direction;
        }

        public void UpdateProperties(
            int x,
            int y,
            int width,
            int height,
            int rotation = 0,
            string? color = null,
            string? text = null,
            string? iconName = null,
            int? thickness = null,
            double? opacity = null,
            string? direction = null
        )
        {
            SetPosition(x, y);
            SetDimensions(width, height);
            SetRotation(rotation);
            SetColor(color);
            SetText(text);
            SetIconName(iconName);
            SetThickness(thickness);
            SetOpacity(opacity);
            SetDirection(direction);
        }
    }
}
