using System;
using MP.Domain.OrganizationalUnits;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace MP.Domain.Tests.OrganizationalUnits
{
    public class RegistrationCodeTests : MPDomainTestBase<MPDomainTestModule>
    {
        private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUnitId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        [Fact]
        public void Should_Create_Registration_Code_With_Valid_Data()
        {
            // Arrange
            var id = Guid.NewGuid();
            var code = "CTO-MAIN-ABC123";

            // Act
            var regCode = new OrganizationalUnitRegistrationCode(id, TestUnitId, code, null, null, null, TestTenantId);

            // Assert
            regCode.Id.ShouldBe(id);
            regCode.Code.ShouldBe(code);
            regCode.OrganizationalUnitId.ShouldBe(TestUnitId);
            regCode.TenantId.ShouldBe(TestTenantId);
            regCode.RoleId.ShouldBeNull();
            regCode.ExpiresAt.ShouldBeNull();
            regCode.MaxUsageCount.ShouldBeNull();
            regCode.UsageCount.ShouldBe(0);
            regCode.LastUsedAt.ShouldBeNull();
            regCode.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Should_Create_Registration_Code_With_Expiry()
        {
            // Arrange
            var id = Guid.NewGuid();
            var code = "CODE-123";
            var expiryDate = DateTime.UtcNow.AddDays(30);

            // Act
            var regCode = new OrganizationalUnitRegistrationCode(id, TestUnitId, code, null, expiryDate, null, TestTenantId);

            // Assert
            regCode.ExpiresAt.ShouldBe(expiryDate);
            regCode.IsExpired().ShouldBeFalse();
        }

        [Fact]
        public void Should_Create_Registration_Code_With_Usage_Limit()
        {
            // Arrange
            var id = Guid.NewGuid();
            var code = "CODE-456";
            var maxUsage = 5;

            // Act
            var regCode = new OrganizationalUnitRegistrationCode(id, TestUnitId, code, null, null, maxUsage, TestTenantId);

            // Assert
            regCode.MaxUsageCount.ShouldBe(maxUsage);
            regCode.IsUsageLimitReached().ShouldBeFalse();
        }

        [Fact]
        public void Should_Throw_When_Code_Is_Null()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, null!, null, null, null, TestTenantId);
            }).Code.ShouldBe("RegistrationCode.CodeRequired");
        }

        [Fact]
        public void Should_Throw_When_Code_Is_Empty()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "", null, null, null, TestTenantId);
            }).Code.ShouldBe("RegistrationCode.CodeRequired");
        }

        [Fact]
        public void Should_Throw_When_Code_Exceeds_Max_Length()
        {
            // Arrange
            var longCode = new string('A', 51);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, longCode, null, null, null, TestTenantId);
            }).Code.ShouldBe("RegistrationCode.CodeTooLong");
        }

        [Fact]
        public void Should_Throw_When_ExpiryDate_Is_In_Past()
        {
            // Arrange
            var pastDate = DateTime.UtcNow.AddDays(-1);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, pastDate, null, TestTenantId);
            }).Code.ShouldBe("RegistrationCode.ExpiryDateInvalid");
        }

        [Fact]
        public void Should_Throw_When_ExpiryDate_Is_Now()
        {
            // Arrange
            var now = DateTime.UtcNow;

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, now, null, TestTenantId);
            }).Code.ShouldBe("RegistrationCode.ExpiryDateInvalid");
        }

        [Fact]
        public void Should_Throw_When_MaxUsageCount_Is_Zero()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, 0, TestTenantId);
            }).Code.ShouldBe("RegistrationCode.MaxUsageCountInvalid");
        }

        [Fact]
        public void Should_Throw_When_MaxUsageCount_Is_Negative()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, -1, TestTenantId);
            }).Code.ShouldBe("RegistrationCode.MaxUsageCountInvalid");
        }

        [Fact]
        public void Should_Increment_Usage_Count()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, 5, TestTenantId);
            regCode.UsageCount.ShouldBe(0);

            // Act
            regCode.IncrementUsageCount();

            // Assert
            regCode.UsageCount.ShouldBe(1);
            regCode.LastUsedAt.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Increment_Usage_Count_Multiple_Times()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, 5, TestTenantId);

            // Act
            regCode.IncrementUsageCount();
            regCode.IncrementUsageCount();
            regCode.IncrementUsageCount();

            // Assert
            regCode.UsageCount.ShouldBe(3);
        }

        [Fact]
        public void Should_Throw_When_Usage_Limit_Reached()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, 1, TestTenantId);
            regCode.IncrementUsageCount();

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                regCode.IncrementUsageCount();
            }).Code.ShouldBe("RegistrationCode.CannotUse");
        }

        [Fact]
        public void Should_Detect_Expired_Code()
        {
            // Arrange
            var pastDate = DateTime.UtcNow.AddSeconds(-1);
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, pastDate, null, TestTenantId);

            // Act - Create code with past expiry date
            var canBeUsed = regCode.CanBeUsed();

            // Assert
            canBeUsed.ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_CanBeUsed_False_When_Inactive()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, null, TestTenantId);
            regCode.Deactivate();

            // Act
            var canBeUsed = regCode.CanBeUsed();

            // Assert
            canBeUsed.ShouldBeFalse();
        }

        [Fact]
        public void Should_Deactivate_Registration_Code()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, null, TestTenantId);
            regCode.IsActive.ShouldBeTrue();

            // Act
            regCode.Deactivate();

            // Assert
            regCode.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Should_Activate_Registration_Code()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, null, TestTenantId);
            regCode.Deactivate();

            // Act
            regCode.Activate();

            // Assert
            regCode.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_CanBeUsed_True_For_Valid_Code()
        {
            // Arrange
            var futureDate = DateTime.UtcNow.AddDays(30);
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, futureDate, 5, TestTenantId);

            // Act
            var canBeUsed = regCode.CanBeUsed();

            // Assert
            canBeUsed.ShouldBeTrue();
        }

        [Fact]
        public void Should_Allow_Unlimited_Usage_When_MaxUsageCount_Is_Null()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, null, TestTenantId);

            // Act & Assert
            for (int i = 0; i < 100; i++)
            {
                regCode.IncrementUsageCount();
            }

            regCode.UsageCount.ShouldBe(100);
        }

        [Fact]
        public void Should_Allow_Unlimited_Expiry_When_ExpiresAt_Is_Null()
        {
            // Arrange
            var regCode = new OrganizationalUnitRegistrationCode(Guid.NewGuid(), TestUnitId, "CODE", null, null, 1, TestTenantId);

            // Act
            regCode.IncrementUsageCount();

            // Assert - should not throw, and code should not be usable anymore
            var canBeUsed = regCode.CanBeUsed();
            canBeUsed.ShouldBeFalse();
        }
    }
}
