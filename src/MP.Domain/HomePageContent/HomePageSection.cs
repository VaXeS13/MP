using System;
using MP.HomePageContent;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.HomePageContent
{
    /// <summary>
    /// Represents a section displayed on the homepage
    /// </summary>
    public class HomePageSection : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }

        /// <summary>
        /// Type of the section (HeroBanner, PromotionCards, etc.)
        /// </summary>
        public HomePageSectionType SectionType { get; private set; }

        /// <summary>
        /// Main title of the section
        /// </summary>
        public string Title { get; private set; } = null!;

        /// <summary>
        /// Optional subtitle
        /// </summary>
        public string? Subtitle { get; private set; }

        /// <summary>
        /// HTML content for the section
        /// </summary>
        public string? Content { get; private set; }

        /// <summary>
        /// ID of the uploaded image file (if any)
        /// </summary>
        public Guid? ImageFileId { get; private set; }

        /// <summary>
        /// URL for CTA link
        /// </summary>
        public string? LinkUrl { get; private set; }

        /// <summary>
        /// Text for CTA button/link
        /// </summary>
        public string? LinkText { get; private set; }

        /// <summary>
        /// Display order (lower number = displayed first)
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// Whether the section is currently active/published
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Start date for scheduled publishing (null = immediate)
        /// </summary>
        public DateTime? ValidFrom { get; private set; }

        /// <summary>
        /// End date for scheduled publishing (null = no expiry)
        /// </summary>
        public DateTime? ValidTo { get; private set; }

        /// <summary>
        /// Background color (hex code, e.g., #FFFFFF)
        /// </summary>
        public string? BackgroundColor { get; private set; }

        /// <summary>
        /// Text color (hex code, e.g., #000000)
        /// </summary>
        public string? TextColor { get; private set; }

        // Constructor for EF Core
        private HomePageSection() { }

        public HomePageSection(
            Guid id,
            HomePageSectionType sectionType,
            string title,
            int order,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            SectionType = sectionType;
            SetTitle(title);
            Order = order;
            IsActive = false; // Default to inactive until explicitly published
        }

        // Setters with validation

        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new BusinessException("HOMEPAGE_SECTION_TITLE_REQUIRED");

            if (title.Length > 200)
                throw new BusinessException("HOMEPAGE_SECTION_TITLE_TOO_LONG");

            Title = title.Trim();
        }

        public void SetSubtitle(string? subtitle)
        {
            if (subtitle != null && subtitle.Length > 500)
                throw new BusinessException("HOMEPAGE_SECTION_SUBTITLE_TOO_LONG");

            Subtitle = subtitle?.Trim();
        }

        public void SetContent(string? content)
        {
            if (content != null && content.Length > 10000)
                throw new BusinessException("HOMEPAGE_SECTION_CONTENT_TOO_LONG");

            Content = content?.Trim();
        }

        public void SetImageFileId(Guid? imageFileId)
        {
            ImageFileId = imageFileId;
        }

        public void SetLink(string? linkUrl, string? linkText)
        {
            if (linkUrl != null && linkUrl.Length > 2000)
                throw new BusinessException("HOMEPAGE_SECTION_LINK_URL_TOO_LONG");

            if (linkText != null && linkText.Length > 100)
                throw new BusinessException("HOMEPAGE_SECTION_LINK_TEXT_TOO_LONG");

            LinkUrl = linkUrl?.Trim();
            LinkText = linkText?.Trim();
        }

        public void SetOrder(int order)
        {
            if (order < 0)
                throw new BusinessException("HOMEPAGE_SECTION_ORDER_CANNOT_BE_NEGATIVE");

            Order = order;
        }

        public void SetValidityPeriod(DateTime? validFrom, DateTime? validTo)
        {
            if (validFrom.HasValue && validTo.HasValue && validFrom.Value >= validTo.Value)
                throw new BusinessException("HOMEPAGE_SECTION_INVALID_VALIDITY_PERIOD");

            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        public void SetColors(string? backgroundColor, string? textColor)
        {
            BackgroundColor = backgroundColor?.Trim();
            TextColor = textColor?.Trim();
        }

        public void SetSectionType(HomePageSectionType sectionType)
        {
            SectionType = sectionType;
        }

        // Activation/Deactivation

        public void Activate()
        {
            if (IsActive)
                throw new BusinessException("HOMEPAGE_SECTION_ALREADY_ACTIVE");

            IsActive = true;
        }

        public void Deactivate()
        {
            if (!IsActive)
                throw new BusinessException("HOMEPAGE_SECTION_ALREADY_INACTIVE");

            IsActive = false;
        }

        // Validation

        public bool IsValidForDisplay()
        {
            if (!IsActive)
                return false;

            var now = DateTime.UtcNow;

            if (ValidFrom.HasValue && now < ValidFrom.Value)
                return false;

            if (ValidTo.HasValue && now > ValidTo.Value)
                return false;

            return true;
        }
    }
}
