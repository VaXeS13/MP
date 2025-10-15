using System;
using MP.HomePageContent;
using Volo.Abp.Application.Dtos;

namespace MP.Application.Contracts.HomePageContent
{
    public class HomePageSectionDto : FullAuditedEntityDto<Guid>
    {
        public HomePageSectionType SectionType { get; set; }
        public string Title { get; set; } = null!;
        public string? Subtitle { get; set; }
        public string? Content { get; set; }
        public Guid? ImageFileId { get; set; }
        public string? LinkUrl { get; set; }
        public string? LinkText { get; set; }
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string? BackgroundColor { get; set; }
        public string? TextColor { get; set; }
        public bool IsValidForDisplay { get; set; }
    }
}
