using System;
using System.ComponentModel.DataAnnotations;
using MP.HomePageContent;

namespace MP.Application.Contracts.HomePageContent
{
    public class CreateHomePageSectionDto
    {
        [Required]
        public HomePageSectionType SectionType { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(500)]
        public string? Subtitle { get; set; }

        [StringLength(10000)]
        public string? Content { get; set; }

        public Guid? ImageFileId { get; set; }

        [StringLength(2000)]
        public string? LinkUrl { get; set; }

        [StringLength(100)]
        public string? LinkText { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        [StringLength(50)]
        public string? BackgroundColor { get; set; }

        [StringLength(50)]
        public string? TextColor { get; set; }
    }
}
