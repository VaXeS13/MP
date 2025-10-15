using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MP.Application.Contracts.Files;
using MP.Domain.Files;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace MP.Application.Files
{
    [Authorize]
    public class UploadedFileAppService : ApplicationService, IUploadedFileAppService
    {
        private readonly IUploadedFileRepository _uploadedFileRepository;
        private readonly IRepository<UploadedFile, Guid> _repository;

        public UploadedFileAppService(
            IUploadedFileRepository uploadedFileRepository,
            IRepository<UploadedFile, Guid> repository)
        {
            _uploadedFileRepository = uploadedFileRepository;
            _repository = repository;
        }

        public async Task<UploadedFileDto> UploadAsync(UploadFileDto input)
        {
            // Decode base64 content
            byte[] fileContent;
            try
            {
                fileContent = Convert.FromBase64String(input.ContentBase64);
            }
            catch (FormatException)
            {
                throw new BusinessException("INVALID_FILE_CONTENT")
                    .WithData("error", "Invalid base64 encoded content");
            }

            // Create the file entity
            var uploadedFile = new UploadedFile(
                GuidGenerator.Create(),
                input.FileName,
                input.ContentType,
                fileContent.Length,
                fileContent,
                CurrentTenant.Id
            );

            if (!string.IsNullOrWhiteSpace(input.Description))
            {
                uploadedFile.SetDescription(input.Description);
            }

            // Save to database
            await _uploadedFileRepository.InsertAsync(uploadedFile, autoSave: true);

            // Map to DTO
            return ObjectMapper.Map<UploadedFile, UploadedFileDto>(uploadedFile);
        }

        public async Task<UploadedFileDto> GetAsync(Guid id)
        {
            var file = await _repository.GetAsync(id);

            var dto = ObjectMapper.Map<UploadedFile, UploadedFileDto>(file);

            // Include base64 content for download
            dto.ContentBase64 = Convert.ToBase64String(file.Content);

            return dto;
        }

        public async Task DeleteAsync(Guid id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
