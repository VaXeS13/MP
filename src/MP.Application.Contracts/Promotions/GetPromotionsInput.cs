using Volo.Abp.Application.Dtos;

namespace MP.Promotions
{
    public class GetPromotionsInput : PagedAndSortedResultRequestDto
    {
        public string? FilterText { get; set; }
        public bool? IsActive { get; set; }
        public PromotionType? Type { get; set; }

        public GetPromotionsInput()
        {
            Sorting = "Priority DESC, CreationTime DESC";
        }
    }
}
