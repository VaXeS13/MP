using System;
using System.Threading.Tasks;
using MP.Domain.Booths;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Domain.Tests.Booths
{
    public class BoothManagerSimpleTests : MPDomainTestBase<MPDomainTestModule>
    {
        private readonly BoothManager _boothManager;
        private readonly IBoothRepository _boothRepository;

        public BoothManagerSimpleTests()
        {
            _boothManager = GetRequiredService<BoothManager>();
            _boothRepository = GetRequiredService<IBoothRepository>();
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Create_Booth()
        {
            // Arrange
            var boothNumber = "BOOTH001";
            var price = 100m;

            // Act
            var booth = await _boothManager.CreateAsync(boothNumber, price);

            // Assert
            booth.ShouldNotBeNull();
            booth.Number.ShouldBe(boothNumber.ToUpper());
            booth.PricePerDay.ShouldBe(price);
        }

        [Fact]
        [UnitOfWork]
        public async Task CreateAsync_Should_Throw_Duplicate_Number()
        {
            // Arrange
            var boothNumber = $"B{Guid.NewGuid().ToString().Substring(0, 4)}";
            var booth1 = await _boothManager.CreateAsync(boothNumber, 100m);
            await _boothRepository.InsertAsync(booth1);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _boothManager.CreateAsync(boothNumber, 150m)
            );

            exception.Code.ShouldBe("BOOTH_NUMBER_ALREADY_EXISTS");
        }

        [Fact]
        [UnitOfWork]
        public async Task ChangeNumberAsync_Should_Update_Number()
        {
            // Arrange
            var oldNumber = $"OLD{Guid.NewGuid().ToString().Substring(0, 4)}";
            var newNumber = $"NEW{Guid.NewGuid().ToString().Substring(0, 4)}";
            var booth = await _boothManager.CreateAsync(oldNumber, 100m);
            await _boothRepository.InsertAsync(booth);

            // Act
            await _boothManager.ChangeNumberAsync(booth, newNumber);
            await _boothRepository.UpdateAsync(booth);

            // Assert
            var updated = await _boothRepository.GetAsync(booth.Id);
            updated.Number.ShouldBe(newNumber.ToUpper());
        }
    }
}
