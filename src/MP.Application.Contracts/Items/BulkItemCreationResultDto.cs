using System.Collections.Generic;

namespace MP.Items
{
    public class BulkItemCreationResultDto
    {
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<ItemDto> CreatedItems { get; set; } = new();
        public List<BulkItemErrorDto> Errors { get; set; } = new();
    }

    public class BulkItemErrorDto
    {
        public int ItemIndex { get; set; }
        public string ItemName { get; set; } = null!;
        public string ErrorMessage { get; set; } = null!;
    }
}
