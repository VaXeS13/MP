using System;
using System.Linq;
using System.Threading.Tasks;
using MP.FloorPlans;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.FloorPlans
{
    public class FloorPlanAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IFloorPlanAppService _floorPlanAppService;

        public FloorPlanAppServiceSimpleTests()
        {
            _floorPlanAppService = GetRequiredService<IFloorPlanAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_FloorPlan()
        {
            // Arrange
            var name = $"FloorPlan_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var createDto = new CreateFloorPlanDto
            {
                Name = name,
                Level = 1,
                Width = 100,
                Height = 100
            };

            // Act
            var result = await _floorPlanAppService.CreateAsync(createDto);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(name);
            result.Level.ShouldBe(1);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_FloorPlan()
        {
            // Arrange
            var name = $"FloorPlan_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = name,
                Level = 2,
                Width = 150,
                Height = 150
            });

            // Act
            var result = await _floorPlanAppService.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
            result.Name.ShouldBe(name);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_FloorPlan()
        {
            // Arrange
            var originalName = $"Original_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = originalName,
                Level = 1,
                Width = 100,
                Height = 100
            });

            var newName = $"Updated_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var updateDto = new UpdateFloorPlanDto
            {
                Name = newName,
                Level = 3,
                Width = 200,
                Height = 200
            };

            // Act
            var result = await _floorPlanAppService.UpdateAsync(created.Id, updateDto);

            // Assert
            result.ShouldNotBeNull();
            result.Name.ShouldBe(newName);
            result.Level.ShouldBe(3);
        }

        [Fact]
        [UnitOfWork]
        public async Task GetListAsync_Should_Return_Paginated_FloorPlans()
        {
            // Arrange
            var name1 = $"FP1_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var name2 = $"FP2_{Guid.NewGuid().ToString().Substring(0, 8)}";

            await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = name1,
                Level = 1,
                Width = 100,
                Height = 100
            });

            await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = name2,
                Level = 2,
                Width = 150,
                Height = 150
            });

            // Act
            var result = await _floorPlanAppService.GetListAsync(new GetFloorPlanListDto());

            // Assert
            result.ShouldNotBeNull();
            result.Items.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task PublishAsync_Should_Publish_FloorPlan()
        {
            // Arrange
            var name = $"FloorPlan_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = name,
                Level = 1,
                Width = 100,
                Height = 100
            });

            // Act
            var result = await _floorPlanAppService.PublishAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeactivateAsync_Should_Deactivate_FloorPlan()
        {
            // Arrange
            var name = $"FloorPlan_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = name,
                Level = 1,
                Width = 100,
                Height = 100
            });

            // Act
            var result = await _floorPlanAppService.DeactivateAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.IsActive.ShouldBeFalse();
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteAsync_Should_Delete_FloorPlan()
        {
            // Arrange
            var name = $"FloorPlan_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = name,
                Level = 1,
                Width = 100,
                Height = 100
            });

            // Act
            await _floorPlanAppService.DeleteAsync(created.Id);

            // Assert
            var exception = await Should.ThrowAsync<Exception>(
                () => _floorPlanAppService.GetAsync(created.Id)
            );
            exception.ShouldNotBeNull();
        }
    }
}
