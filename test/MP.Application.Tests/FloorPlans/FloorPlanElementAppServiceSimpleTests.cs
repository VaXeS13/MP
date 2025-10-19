using System;
using System.Linq;
using System.Threading.Tasks;
using MP.FloorPlans;
using MP.Domain.FloorPlans;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.FloorPlans
{
    public class FloorPlanElementAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IFloorPlanAppService _floorPlanAppService;
        private readonly IFloorPlanElementAppService _floorPlanElementAppService;

        public FloorPlanElementAppServiceSimpleTests()
        {
            _floorPlanAppService = GetRequiredService<IFloorPlanAppService>();
            _floorPlanElementAppService = GetRequiredService<IFloorPlanElementAppService>();
        }

        private async Task<Guid> CreateTestFloorPlanAsync()
        {
            var name = $"FloorPlan_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var created = await _floorPlanAppService.CreateAsync(new CreateFloorPlanDto
            {
                Name = name,
                Level = 1,
                Width = 100,
                Height = 100
            });
            return created.Id;
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_Element()
        {
            // Arrange
            var floorPlanId = await CreateTestFloorPlanAsync();
            var createDto = new CreateFloorPlanElementDto
            {
                ElementType = FloorPlanElementType.Wall,
                Text = $"Element_{Guid.NewGuid().ToString().Substring(0, 8)}",
                X = 10,
                Y = 10,
                Width = 20,
                Height = 5
            };

            // Act
            var result = await _floorPlanElementAppService.CreateAsync(floorPlanId, createDto);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAsync_Should_Return_Element()
        {
            // Arrange
            var floorPlanId = await CreateTestFloorPlanAsync();
            var created = await _floorPlanElementAppService.CreateAsync(floorPlanId, new CreateFloorPlanElementDto
            {
                ElementType = FloorPlanElementType.Door,
                Text = $"Element_{Guid.NewGuid().ToString().Substring(0, 8)}",
                X = 5,
                Y = 5,
                Width = 10,
                Height = 2
            });

            // Act
            var result = await _floorPlanElementAppService.GetAsync(created.Id);

            // Assert
            result.ShouldNotBeNull();
            result.Id.ShouldBe(created.Id);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_Element()
        {
            // Arrange
            var floorPlanId = await CreateTestFloorPlanAsync();
            var created = await _floorPlanElementAppService.CreateAsync(floorPlanId, new CreateFloorPlanElementDto
            {
                ElementType = FloorPlanElementType.Wall,
                Text = "OriginalName",
                X = 0,
                Y = 0,
                Width = 50,
                Height = 5
            });

            var updateDto = new UpdateFloorPlanElementDto
            {
                ElementType = FloorPlanElementType.Door,
                Text = "UpdatedName",
                X = 10,
                Y = 10,
                Width = 30,
                Height = 3
            };

            // Act
            var result = await _floorPlanElementAppService.UpdateAsync(created.Id, updateDto);

            // Assert
            result.ShouldNotBeNull();
            result.Text.ShouldBe("UpdatedName");
        }

        [Fact]
        [UnitOfWork]
        public async Task GetListByFloorPlanAsync_Should_Return_Elements()
        {
            // Arrange
            var floorPlanId = await CreateTestFloorPlanAsync();
            await _floorPlanElementAppService.CreateAsync(floorPlanId, new CreateFloorPlanElementDto
            {
                ElementType = FloorPlanElementType.Wall,
                Text = $"Element1_{Guid.NewGuid().ToString().Substring(0, 8)}",
                X = 0,
                Y = 0,
                Width = 10,
                Height = 10
            });

            // Act
            var result = await _floorPlanElementAppService.GetListByFloorPlanAsync(floorPlanId);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        [UnitOfWork]
        public async Task DeleteAsync_Should_Delete_Element()
        {
            // Arrange
            var floorPlanId = await CreateTestFloorPlanAsync();
            var created = await _floorPlanElementAppService.CreateAsync(floorPlanId, new CreateFloorPlanElementDto
            {
                ElementType = FloorPlanElementType.Wall,
                Text = $"Element_{Guid.NewGuid().ToString().Substring(0, 8)}",
                X = 5,
                Y = 5,
                Width = 15,
                Height = 2
            });

            // Act
            await _floorPlanElementAppService.DeleteAsync(created.Id);

            // Assert
            var exception = await Should.ThrowAsync<Exception>(
                () => _floorPlanElementAppService.GetAsync(created.Id)
            );
            exception.ShouldNotBeNull();
        }
    }
}
