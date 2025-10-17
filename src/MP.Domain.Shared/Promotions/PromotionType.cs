namespace MP.Promotions
{
    /// <summary>
    /// Defines the type of promotion
    /// </summary>
    public enum PromotionType
    {
        /// <summary>
        /// Quantity-based discount (e.g., buy 3 booths get 10% off)
        /// </summary>
        Quantity = 0,

        /// <summary>
        /// Promo code discount (user enters code to get discount)
        /// </summary>
        PromoCode = 1,

        /// <summary>
        /// Date range promotion (discount valid for specific period)
        /// </summary>
        DateRange = 2,

        /// <summary>
        /// New user promotion (applies to accounts younger than specified days)
        /// </summary>
        NewUser = 3
    }
}
