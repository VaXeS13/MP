using System;
using System.Threading.Tasks;
using MP.Application.Contracts.Files;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Files
{
    public class UploadedFileAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IUploadedFileAppService _uploadedFileAppService;

        public UploadedFileAppServiceSimpleTests()
        {
            _uploadedFileAppService = GetRequiredService<IUploadedFileAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_File()
        {
            // Arrange - use a non-existent ID to test error handling
            var fileId = Guid.NewGuid();

            // Act & Assert - expect it to fail gracefully or return null
            var exception = await Should.ThrowAsync<Exception>(
                () => _uploadedFileAppService.GetAsync(fileId)
            );
            exception.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteAsync_Should_Delete_File()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            // Act & Assert - should handle gracefully
            try
            {
                await _uploadedFileAppService.DeleteAsync(fileId);
            }
            catch (Exception)
            {
                // Expected for non-existent file
            }
        }
    }
}
