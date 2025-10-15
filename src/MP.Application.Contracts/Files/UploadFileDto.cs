using System.ComponentModel.DataAnnotations;

namespace MP.Application.Contracts.Files
{
    public class UploadFileDto
    {
        [Required]
        public string FileName { get; set; } = null!;

        [Required]
        public string ContentType { get; set; } = null!;

        [Required]
        public string ContentBase64 { get; set; } = null!;

        public string? Description { get; set; }
    }
}
