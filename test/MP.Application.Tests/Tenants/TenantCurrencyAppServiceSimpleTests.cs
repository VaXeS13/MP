using System.Threading.Tasks;
using MP.Tenants;
using MP.Domain.Booths;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Tenants
{
    public class TenantCurrencyAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly ITenantCurrencyAppService _tenantCurrencyAppService;

        public TenantCurrencyAppServiceSimpleTests()
        {
            _tenantCurrencyAppService = GetRequiredService<ITenantCurrencyAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetTenantCurrencyAsync_Should_Return_Tenant_Currency()
        {
            // Act
            var result = await _tenantCurrencyAppService.GetTenantCurrencyAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task UpdateTenantCurrencyAsync_Should_Update_Tenant_Currency()
        {
            // Arrange
            var updateDto = new TenantCurrencyDto
            {
                Currency = Currency.EUR
            };

            // Act
            await _tenantCurrencyAppService.UpdateTenantCurrencyAsync(updateDto);

            // Assert
            var result = await _tenantCurrencyAppService.GetTenantCurrencyAsync();
            result.Currency.ShouldBe(Currency.EUR);
        }
    }
}
