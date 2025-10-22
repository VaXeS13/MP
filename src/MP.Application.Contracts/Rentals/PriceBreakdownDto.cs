using System.Collections.Generic;
using System.Linq;

namespace MP.Application.Contracts.Rentals
{
    /// <summary>
    /// DTO representing how rental price was calculated from multi-period pricing
    /// </summary>
    public class PriceBreakdownDto
    {
        /// <summary>
        /// List of pricing period usages
        /// </summary>
        public List<PriceBreakdownItemDto> Items { get; set; } = new();

        /// <summary>
        /// Total calculated price
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Formatted breakdown string for display
        /// Example: "2×7 dni (6zł) + 2×1 dzień (1zł) = 14zł"
        /// </summary>
        public string FormattedBreakdown { get; set; } = string.Empty;

        /// <summary>
        /// Simple formatted string for UI
        /// Example: "14zł za 16 dni"
        /// </summary>
        public string SimpleSummary { get; set; } = string.Empty;

        public PriceBreakdownDto()
        {
        }

        public PriceBreakdownDto(decimal totalPrice, List<PriceBreakdownItemDto> items)
        {
            TotalPrice = totalPrice;
            Items = items;
            FormattedBreakdown = GenerateFormattedBreakdown();
            SimpleSummary = GenerateSimpleSummary();
        }

        private string GenerateFormattedBreakdown()
        {
            if (Items == null || Items.Count == 0)
                return string.Empty;

            var parts = Items.Select(item =>
                $"{item.Count}×{item.Days} {(item.Days == 1 ? "dzień" : "dni")} ({item.PricePerPeriod}zł)"
            );

            return $"{string.Join(" + ", parts)} = {TotalPrice}zł";
        }

        private string GenerateSimpleSummary()
        {
            if (Items == null || Items.Count == 0)
                return $"{TotalPrice}zł";

            var totalDays = Items.Sum(i => i.Count * i.Days);
            return $"{TotalPrice}zł za {totalDays} {(totalDays == 1 ? "dzień" : "dni")}";
        }
    }

    /// <summary>
    /// Single pricing period usage in the breakdown
    /// </summary>
    public class PriceBreakdownItemDto
    {
        /// <summary>
        /// Number of days in this pricing period (e.g., 7 for weekly)
        /// </summary>
        public int Days { get; set; }

        /// <summary>
        /// How many times this period was used (e.g., 2 weeks)
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Price per single period (e.g., 6zł for 7-day period)
        /// </summary>
        public decimal PricePerPeriod { get; set; }

        /// <summary>
        /// Subtotal for this period usage (Count × PricePerPeriod)
        /// </summary>
        public decimal Subtotal { get; set; }

        public PriceBreakdownItemDto()
        {
        }

        public PriceBreakdownItemDto(int days, int count, decimal pricePerPeriod, decimal subtotal)
        {
            Days = days;
            Count = count;
            PricePerPeriod = pricePerPeriod;
            Subtotal = subtotal;
        }
    }
}
