using System.ComponentModel;

namespace MP.Domain.Booths
{
    public enum Currency
    {
        [Description("PLN")]
        PLN = 1,

        [Description("EUR")]
        EUR = 2,

        [Description("USD")]
        USD = 3,

        [Description("GBP")]
        GBP = 4,

        [Description("CZK")]
        CZK = 5
    }
}