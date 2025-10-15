using System;
using Volo.Abp.Application.Dtos;

namespace MP.Application.Contracts.Files
{
    public class UploadedFileDto : FullAuditedEntityDto<Guid>
    {
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// Base64 encoded content for transferring file data
        /// </summary>
        public string? ContentBase64 { get; set; }
    }
}
