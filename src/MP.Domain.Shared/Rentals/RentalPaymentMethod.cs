namespace MP.Domain.Rentals
{
    public enum RentalPaymentMethod
    {
        Online = 0,     // Online payment via payment gateway
        Cash = 1,       // Cash payment on-site
        Terminal = 2,   // Card payment via terminal on-site
        Free = 3        // Free (gratis) - no payment required
    }
}
