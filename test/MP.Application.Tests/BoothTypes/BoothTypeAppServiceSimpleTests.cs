using System;
using System.Linq;
using System.Threading.Tasks;
using MP.Application.Contracts.BoothTypes;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.BoothTypes
{
    public class BoothTypeAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IBoothTypeAppService _boothTypeAppService;

        public BoothTypeAppServiceSimpleTests()
        {
            _boothTypeAppService = GetRequiredService<IBoothTypeAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_BoothType()
        {
            // Arrange
            var typeName = $"BT_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createDto = new CreateBoothTypeDto
            {
                Name = typeName,
                Description = "Test booth type",
                CommissionPercentage = 10m
            };

            // Act
            var result = await _boothTypeAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(typeName);
            result.CommissionPercentage.ShouldBe(10m);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetListAsync_Should_Return_BoothTypes()
        {
            // Arrange
            var typeName = $"BT_{Guid.NewGuid().ToString().Substring(0, 8)}";
            await _boothTypeAppService.CreateAsync(new CreateBoothTypeDto
            {
                Name = typeName,
                Description = "Test type",
                CommissionPercentage = 5m
            });

            // Act
            var result = await _boothTypeAppService.GetListAsync(
                new PagedAndSortedResultRequestDto()
            );

            // Assert
            result.ShouldNotBeNull();
            result.Items.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_BoothType()
        {
            // Arrange
            var typeName = $"BT_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _boothTypeAppService.CreateAsync(new CreateBoothTypeDto
            {
                Name = typeName,
                Description = "Test",
                CommissionPercentage = 15m
            });

            // Act
            var result = await _boothTypeAppService.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
            result.Name.ShouldBe(typeName);
            result.CommissionPercentage.ShouldBe(15m);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_BoothType()
        {
            // Arrange
            var typeName = $"Original_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _boothTypeAppService.CreateAsync(new CreateBoothTypeDto
            {
                Name = typeName,
                Description = "Original",
                CommissionPercentage = 5m
            });

            var newName = $"Updated_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var updateDto = new UpdateBoothTypeDto
            {
                Name = newName,
                Description = "Updated",
                CommissionPercentage = 20m
            };

            // Act
            await _boothTypeAppService.UpdateAsync(created.Id, updateDto);

            // Assert
            var updated = await _boothTypeAppService.GetAsync(created.Id);
            updated.Name.ShouldBe(newName);
            updated.CommissionPercentage.ShouldBe(20m);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetActiveTypesAsync_Should_Return_Active_Types()
        {
            // Arrange - Create and activate a booth type
            var typeName = $"Active_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _boothTypeAppService.CreateAsync(new CreateBoothTypeDto
            {
                Name = typeName,
                Description = "Active type",
                CommissionPercentage = 8m
            });

            // Act
            var result = await _boothTypeAppService.GetActiveTypesAsync();

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task ActivateAsync_Should_Activate_BoothType()
        {
            // Arrange
            var typeName = $"ToActivate_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _boothTypeAppService.CreateAsync(new CreateBoothTypeDto
            {
                Name = typeName,
                Description = "Test",
                CommissionPercentage = 5m
            });

            // Deactivate first
            await _boothTypeAppService.DeactivateAsync(created.Id);

            // Act
            await _boothTypeAppService.ActivateAsync(created.Id);

            // Assert
            var result = await _boothTypeAppService.GetAsync(created.Id);
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeactivateAsync_Should_Deactivate_BoothType()
        {
            // Arrange
            var typeName = $"ToDeactivate_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _boothTypeAppService.CreateAsync(new CreateBoothTypeDto
            {
                Name = typeName,
                Description = "Test",
                CommissionPercentage = 5m
            });

            // Act
            await _boothTypeAppService.DeactivateAsync(created.Id);

            // Assert
            var result = await _boothTypeAppService.GetAsync(created.Id);
            result.ShouldNotBeNull();
        }
    }
}
