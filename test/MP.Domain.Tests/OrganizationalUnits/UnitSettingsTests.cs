using System;
using System.Collections.Generic;
using MP.Domain.OrganizationalUnits;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace MP.Domain.Tests.OrganizationalUnits
{
    public class UnitSettingsTests : MPDomainTestBase<MPDomainTestModule>
    {
        private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUnitId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        [Fact]
        public void Should_Create_Settings_With_Defaults()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var settings = new OrganizationalUnitSettings(id, TestUnitId, TestTenantId);

            // Assert
            settings.Id.ShouldBe(id);
            settings.OrganizationalUnitId.ShouldBe(TestUnitId);
            settings.TenantId.ShouldBe(TestTenantId);
            settings.Currency.ShouldBe("PLN");
            settings.DefaultPaymentProvider.ShouldBe("stripe");
            settings.LogoUrl.ShouldBeNull();
            settings.BannerText.ShouldBeNull();
            settings.IsMainUnit.ShouldBeFalse();
        }

        [Fact]
        public void Should_Create_MainUnit_Settings()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var settings = new OrganizationalUnitSettings(id, TestUnitId, TestTenantId, isMainUnit: true);

            // Assert
            settings.IsMainUnit.ShouldBeTrue();
        }

        [Theory]
        [InlineData("PLN")]
        [InlineData("EUR")]
        [InlineData("USD")]
        [InlineData("GBP")]
        [InlineData("CZK")]
        public void Should_Update_Currency_With_Valid_Values(string currency)
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);

            // Act
            settings.UpdateCurrency(currency);

            // Assert
            settings.Currency.ShouldBe(currency);
        }

        [Fact]
        public void Should_Throw_When_Currency_Is_Null()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                settings.UpdateCurrency(null!);
            }).Code.ShouldBe("UnitSettings.CurrencyRequired");
        }

        [Fact]
        public void Should_Throw_When_Currency_Is_Invalid()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                settings.UpdateCurrency("XYZ");
            }).Code.ShouldBe("UnitSettings.CurrencyInvalid");
        }

        [Fact]
        public void Should_Update_Payment_Providers()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var providers = new Dictionary<string, bool>
            {
                { "stripe", false },
                { "p24", true },
                { "paypal", true }
            };

            // Act
            settings.UpdatePaymentProviders(providers);

            // Assert
            settings.IsPaymentProviderEnabled("p24").ShouldBeTrue();
            settings.IsPaymentProviderEnabled("paypal").ShouldBeTrue();
            settings.IsPaymentProviderEnabled("stripe").ShouldBeFalse();
        }

        [Fact]
        public void Should_Throw_When_No_Providers_Enabled()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var providers = new Dictionary<string, bool>
            {
                { "stripe", false },
                { "p24", false },
                { "paypal", false }
            };

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                settings.UpdatePaymentProviders(providers);
            }).Code.ShouldBe("UnitSettings.NoProvidersEnabled");
        }

        [Fact]
        public void Should_Throw_When_Invalid_Provider_Name()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var providers = new Dictionary<string, bool>
            {
                { "invalid_provider", true }
            };

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                settings.UpdatePaymentProviders(providers);
            }).Code.ShouldBe("UnitSettings.ProviderInvalid");
        }

        [Fact]
        public void Should_Set_Default_Payment_Provider()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var providers = new Dictionary<string, bool>
            {
                { "stripe", true },
                { "p24", true },
                { "paypal", false }
            };
            settings.UpdatePaymentProviders(providers);

            // Act
            settings.SetDefaultPaymentProvider("p24");

            // Assert
            settings.DefaultPaymentProvider.ShouldBe("p24");
        }

        [Fact]
        public void Should_Throw_When_Setting_Disabled_Provider_As_Default()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var providers = new Dictionary<string, bool>
            {
                { "stripe", true },
                { "p24", false },
                { "paypal", false }
            };
            settings.UpdatePaymentProviders(providers);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                settings.SetDefaultPaymentProvider("p24");
            }).Code.ShouldBe("UnitSettings.ProviderNotEnabled");
        }

        [Fact]
        public void Should_Clear_Default_Payment_Provider_When_Set_To_Null()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            settings.DefaultPaymentProvider.ShouldNotBeNull();

            // Act
            settings.SetDefaultPaymentProvider(null);

            // Assert
            settings.DefaultPaymentProvider.ShouldBeNull();
        }

        [Fact]
        public void Should_Update_Branding()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var logoUrl = "https://example.com/logo.png";
            var bannerText = "Welcome to our unit!";

            // Act
            settings.UpdateBranding(logoUrl, bannerText);

            // Assert
            settings.LogoUrl.ShouldBe(logoUrl);
            settings.BannerText.ShouldBe(bannerText);
        }

        [Fact]
        public void Should_Throw_When_LogoUrl_Too_Long()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var longUrl = new string('a', 501);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                settings.UpdateBranding(longUrl, null);
            }).Code.ShouldBe("UnitSettings.LogoUrlTooLong");
        }

        [Fact]
        public void Should_Throw_When_BannerText_Too_Long()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var longText = new string('a', 1001);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                settings.UpdateBranding(null, longText);
            }).Code.ShouldBe("UnitSettings.BannerTextTooLong");
        }

        [Fact]
        public void Should_Check_If_Provider_Enabled()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var providers = new Dictionary<string, bool>
            {
                { "stripe", true },
                { "p24", false },
                { "paypal", true }
            };
            settings.UpdatePaymentProviders(providers);

            // Act & Assert
            settings.IsPaymentProviderEnabled("stripe").ShouldBeTrue();
            settings.IsPaymentProviderEnabled("p24").ShouldBeFalse();
            settings.IsPaymentProviderEnabled("paypal").ShouldBeTrue();
        }

        [Fact]
        public void Should_Get_Payment_Providers_Dictionary()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            var providers = new Dictionary<string, bool>
            {
                { "stripe", true },
                { "p24", true },
                { "paypal", false }
            };
            settings.UpdatePaymentProviders(providers);

            // Act
            var result = settings.GetPaymentProviders();

            // Assert
            result.ShouldNotBeNull();
            result!.Count.ShouldBe(3);
            result!["stripe"].ShouldBeTrue();
        }

        [Fact]
        public void Should_Reset_Default_Provider_When_Disabled()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            settings.SetDefaultPaymentProvider("stripe");
            settings.DefaultPaymentProvider.ShouldBe("stripe");

            var providers = new Dictionary<string, bool>
            {
                { "stripe", false },
                { "p24", true },
                { "paypal", false }
            };

            // Act
            settings.UpdatePaymentProviders(providers);

            // Assert
            settings.DefaultPaymentProvider.ShouldBeNull();
        }

        [Fact]
        public void Should_Accept_Null_Logo_And_Banner()
        {
            // Arrange
            var settings = new OrganizationalUnitSettings(Guid.NewGuid(), TestUnitId, TestTenantId);
            settings.UpdateBranding("https://example.com/logo.png", "Banner text");

            // Act
            settings.UpdateBranding(null, null);

            // Assert
            settings.LogoUrl.ShouldBeNull();
            settings.BannerText.ShouldBeNull();
        }
    }
}
