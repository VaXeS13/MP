namespace MP.Promotions
{
    /// <summary>
    /// Defines how discount value is calculated
    /// </summary>
    public enum DiscountType
    {
        /// <summary>
        /// Discount as percentage of total amount (e.g., 10% off)
        /// </summary>
        Percentage = 0,

        /// <summary>
        /// Fixed amount discount (e.g., 50 PLN off)
        /// </summary>
        FixedAmount = 1
    }
}
