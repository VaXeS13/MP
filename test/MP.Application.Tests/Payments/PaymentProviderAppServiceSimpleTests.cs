using System.Threading.Tasks;
using MP.Application.Contracts.Payments;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace MP.Application.Tests.Payments
{
    public class PaymentProviderAppServiceSimpleTests : MPApplicationTestBase<MPApplicationTestModule>
    {
        private readonly IPaymentProviderAppService _paymentProviderAppService;

        public PaymentProviderAppServiceSimpleTests()
        {
            _paymentProviderAppService = GetRequiredService<IPaymentProviderAppService>();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetAvailableProvidersAsync_Should_Return_Providers()
        {
            // Act
            var result = await _paymentProviderAppService.GetAvailableProvidersAsync();

            // Assert
            result.ShouldNotBeNull();
        }

        [Fact]
        [UnitOfWork]
        public async Task GetPaymentMethodsAsync_Should_Return_Methods()
        {
            // Act
            var result = await _paymentProviderAppService.GetPaymentMethodsAsync("p24", "PLN");

            // Assert
            result.ShouldNotBeNull();
        }
    }
}
