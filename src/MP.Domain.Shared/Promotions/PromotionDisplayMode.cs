namespace MP.Promotions
{
    /// <summary>
    /// Defines how promotion notification is displayed to customers
    /// </summary>
    public enum PromotionDisplayMode
    {
        /// <summary>
        /// Don't display notification (promotion still applies if conditions met)
        /// </summary>
        None = 0,

        /// <summary>
        /// Sticky widget at bottom right corner
        /// </summary>
        StickyBottomRight = 1,

        /// <summary>
        /// Sticky widget at bottom left corner
        /// </summary>
        StickyBottomLeft = 2,

        /// <summary>
        /// Modal popup dialog
        /// </summary>
        Popup = 3,

        /// <summary>
        /// Banner at top of the page
        /// </summary>
        Banner = 4
    }
}
