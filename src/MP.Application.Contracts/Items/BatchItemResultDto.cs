using System;

namespace MP.Items
{
    public class BatchItemResultDto
    {
        public Guid ItemId { get; set; }

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
