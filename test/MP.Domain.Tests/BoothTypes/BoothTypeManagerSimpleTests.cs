using System;
using System.Threading.Tasks;
using MP.Domain.BoothTypes;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Domain.Tests.BoothTypes
{
    public class BoothTypeManagerSimpleTests : MPDomainTestBase<MPDomainTestModule>
    {
        private readonly BoothTypeManager _boothTypeManager;
        private readonly IBoothTypeRepository _boothTypeRepository;

        public BoothTypeManagerSimpleTests()
        {
            _boothTypeManager = GetRequiredService<BoothTypeManager>();
            _boothTypeRepository = GetRequiredService<IBoothTypeRepository>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_BoothType()
        {
            // Arrange
            var typeName = $"BT_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Act
            var boothType = await _boothTypeManager.CreateAsync(
                typeName,
                "Test booth type",
                10m
            );

            // Assert
            boothType.ShouldNotBeNull();
            boothType.Name.ShouldBe(typeName);
            boothType.CommissionPercentage.ShouldBe(10m);
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Generate_Unique_Id()
        {
            // Arrange
            var type1Name = $"BT1_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var type2Name = $"BT2_{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Act
            var type1 = await _boothTypeManager.CreateAsync(type1Name, "Desc1", 5m);
            var type2 = await _boothTypeManager.CreateAsync(type2Name, "Desc2", 10m);

            // Assert
            type1.Id.ShouldNotBe(type2.Id);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Update_Properties()
        {
            // Arrange
            var origName = $"ORG_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var newName = $"UPD_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var boothType = await _boothTypeManager.CreateAsync(origName, "Desc", 15m);
            await _boothTypeRepository.InsertAsync(boothType);

            // Act
            await _boothTypeManager.UpdateAsync(
                boothType,
                newName,
                "New Description",
                20m
            );
            await _boothTypeRepository.UpdateAsync(boothType);

            // Assert
            var updated = await _boothTypeRepository.GetAsync(boothType.Id);
            updated.Name.ShouldBe(newName);
            updated.CommissionPercentage.ShouldBe(20m);
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateAsync_Should_Throw_Duplicate_Name()
        {
            // Arrange
            var type1Name = $"TP1_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var type2Name = $"TP2_{Guid.NewGuid().ToString().Substring(0, 8)}";
            var type1 = await _boothTypeManager.CreateAsync(type1Name, "Desc1", 5m);
            var type2 = await _boothTypeManager.CreateAsync(type2Name, "Desc2", 10m);
            await _boothTypeRepository.InsertAsync(type1);
            await _boothTypeRepository.InsertAsync(type2);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _boothTypeManager.UpdateAsync(type1, type2Name, "Desc", 8m)
            );

            exception.Code.ShouldBe("BOOTH_TYPE_NAME_ALREADY_EXISTS");
        }
    }
}
