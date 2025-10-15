using System;
using System.ComponentModel.DataAnnotations;

namespace MP.Application.Contracts.HomePageContent
{
    public class ReorderSectionDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Order { get; set; }
    }
}
