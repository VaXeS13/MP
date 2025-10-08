namespace MP.Carts
{
    public enum CartStatus
    {
        /// <summary>
        /// Cart is active and can be modified
        /// </summary>
        Active = 0,

        /// <summary>
        /// Cart has been checked out and converted to rentals
        /// </summary>
        CheckedOut = 1,

        /// <summary>
        /// Cart was abandoned by user
        /// </summary>
        Abandoned = 2
    }
}