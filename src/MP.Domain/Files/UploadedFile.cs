using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace MP.Domain.Files
{
    /// <summary>
    /// Represents a file uploaded and stored in the database
    /// </summary>
    public class UploadedFile : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; private set; }

        /// <summary>
        /// Original filename uploaded by user
        /// </summary>
        public string FileName { get; private set; } = null!;

        /// <summary>
        /// File content type (MIME type)
        /// </summary>
        public string ContentType { get; private set; } = null!;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Binary content of the file
        /// </summary>
        public byte[] Content { get; private set; } = null!;

        /// <summary>
        /// Optional description of the file
        /// </summary>
        public string? Description { get; private set; }

        // Constructor for EF Core
        private UploadedFile() { }

        public UploadedFile(
            Guid id,
            string fileName,
            string contentType,
            long fileSize,
            byte[] content,
            Guid? tenantId = null) : base(id)
        {
            TenantId = tenantId;
            SetFileName(fileName);
            SetContentType(contentType);
            SetFileSize(fileSize);
            SetContent(content);
        }

        // Setters with validation

        public void SetFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new BusinessException("FILE_NAME_REQUIRED");

            if (fileName.Length > 255)
                throw new BusinessException("FILE_NAME_TOO_LONG");

            FileName = fileName.Trim();
        }

        public void SetContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                throw new BusinessException("FILE_CONTENT_TYPE_REQUIRED");

            if (contentType.Length > 100)
                throw new BusinessException("FILE_CONTENT_TYPE_TOO_LONG");

            ContentType = contentType.Trim();
        }

        public void SetFileSize(long fileSize)
        {
            if (fileSize <= 0)
                throw new BusinessException("FILE_SIZE_MUST_BE_POSITIVE");

            // Maximum file size: 10 MB
            if (fileSize > 10 * 1024 * 1024)
                throw new BusinessException("FILE_SIZE_EXCEEDS_LIMIT");

            FileSize = fileSize;
        }

        public void SetContent(byte[] content)
        {
            if (content == null || content.Length == 0)
                throw new BusinessException("FILE_CONTENT_REQUIRED");

            Content = content;
        }

        public void SetDescription(string? description)
        {
            if (description != null && description.Length > 500)
                throw new BusinessException("FILE_DESCRIPTION_TOO_LONG");

            Description = description?.Trim();
        }

        /// <summary>
        /// Validates if the file is an image based on content type
        /// </summary>
        public bool IsImage()
        {
            return ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the file extension from filename
        /// </summary>
        public string GetExtension()
        {
            var lastDotIndex = FileName.LastIndexOf('.');
            return lastDotIndex >= 0 ? FileName.Substring(lastDotIndex) : string.Empty;
        }
    }
}
