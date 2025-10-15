namespace MP.HomePageContent
{
    /// <summary>
    /// Defines the type of homepage section
    /// </summary>
    public enum HomePageSectionType
    {
        /// <summary>
        /// Hero banner with large image, title, subtitle and CTA button
        /// </summary>
        HeroBanner = 1,

        /// <summary>
        /// Grid of promotion cards with images and descriptions
        /// </summary>
        PromotionCards = 2,

        /// <summary>
        /// Announcement banner with colored background
        /// </summary>
        Announcement = 3,

        /// <summary>
        /// Feature highlights section (grid of features/benefits)
        /// </summary>
        FeatureHighlights = 4,

        /// <summary>
        /// Custom HTML content for advanced users
        /// </summary>
        CustomHtml = 5,

        /// <summary>
        /// Image gallery section
        /// </summary>
        ImageGallery = 6
    }
}
