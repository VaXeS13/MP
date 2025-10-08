using System.ComponentModel.DataAnnotations;

namespace MP.Booths
{
    public class BoothSettingsDto
    {
        /// <summary>
        /// Minimum gap in days between rentals.
        /// If a booth is rented until day D, the next rental can start on day D+1
        /// OR day D+1+MinimumGapDays (leaving at least MinimumGapDays free).
        /// </summary>
        [Range(0, 30)]
        public int MinimumGapDays { get; set; }
    }
}
