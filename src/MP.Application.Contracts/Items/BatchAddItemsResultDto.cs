using System.Collections.Generic;

namespace MP.Items
{
    public class BatchAddItemsResultDto
    {
        public List<BatchItemResultDto> Results { get; set; }

        public int SuccessCount => Results?.FindAll(r => r.Success)?.Count ?? 0;

        public int FailureCount => Results?.FindAll(r => !r.Success)?.Count ?? 0;

        public BatchAddItemsResultDto()
        {
            Results = new List<BatchItemResultDto>();
        }
    }
}
