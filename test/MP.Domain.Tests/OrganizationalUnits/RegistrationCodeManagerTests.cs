using System;
using MP.Domain.OrganizationalUnits;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace MP.Domain.Tests.OrganizationalUnits
{
    /// <summary>
    /// Unit tests for RegistrationCodeManager domain service.
    /// These tests focus on the code generation and validation logic without database dependencies.
    /// </summary>
    public class RegistrationCodeManagerTests
    {
        private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUnitId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid TestRoleId = Guid.Parse("00000000-0000-0000-0000-000000000003");

        private readonly IOrganizationalUnitRegistrationCodeRepository _codeRepository;
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentTenant _currentTenant;
        private readonly RegistrationCodeManager _registrationCodeManager;

        public RegistrationCodeManagerTests()
        {
            _codeRepository = Substitute.For<IOrganizationalUnitRegistrationCodeRepository>();
            _unitRepository = Substitute.For<IOrganizationalUnitRepository>();
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _currentTenant = Substitute.For<ICurrentTenant>();

            // Configure mocks
            _currentTenant.Id.Returns(TestTenantId);
            _guidGenerator.Create().Returns(x => Guid.NewGuid());

            _registrationCodeManager = new RegistrationCodeManager(
                _codeRepository,
                _unitRepository,
                _guidGenerator,
                _currentTenant);
        }

        #region GenerateCodeFormat Tests

        [Fact]
        public void GenerateCodeFormat_Should_Return_Valid_Format()
        {
            // Act
            var code = _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN", 6);

            // Assert
            code.ShouldNotBeNullOrWhiteSpace();
            var parts = code.Split('-');
            parts.Length.ShouldBe(3);
            parts[0].ShouldBe("HOST");
            parts[1].ShouldBe("MAIN");
            parts[2].Length.ShouldBe(6);
            // Random part should contain only alphanumeric characters
            parts[2].ShouldMatch(@"^[A-Z0-9]+$");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Throw_When_TenantCode_Null()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat(null, "MAIN", 6))
                .Code.ShouldBe("RegistrationCode.TenantCodeRequired");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Throw_When_TenantCode_Empty()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat("", "MAIN", 6))
                .Code.ShouldBe("RegistrationCode.TenantCodeRequired");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Throw_When_UnitCode_Null()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat("HOST", null, 6))
                .Code.ShouldBe("RegistrationCode.UnitCodeRequired");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Throw_When_UnitCode_Empty()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat("HOST", "", 6))
                .Code.ShouldBe("RegistrationCode.UnitCodeRequired");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Throw_When_RandomLength_Zero()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN", 0))
                .Code.ShouldBe("RegistrationCode.InvalidRandomLength");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Throw_When_RandomLength_TooLarge()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN", 11))
                .Code.ShouldBe("RegistrationCode.InvalidRandomLength");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Normalize_To_Uppercase()
        {
            // Act
            var code = _registrationCodeManager.GenerateCodeFormat("host", "main", 6);

            // Assert
            var parts = code.Split('-');
            parts[0].ShouldBe("HOST");
            parts[1].ShouldBe("MAIN");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Generate_Different_Random_Parts()
        {
            // Act
            var code1 = _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN", 6);
            var code2 = _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN", 6);

            // Assert
            code1.ShouldNotBe(code2);
        }

        [Fact]
        public void GenerateCodeFormat_Should_Validate_Tenant_Code_Format()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat("HOST@", "MAIN", 6))
                .Code.ShouldBe("RegistrationCode.InvalidTenantCode");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Validate_Unit_Code_Format()
        {
            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN!", 6))
                .Code.ShouldBe("RegistrationCode.InvalidUnitCode");
        }

        [Fact]
        public void GenerateCodeFormat_Should_Support_Hyphens_In_Codes()
        {
            // Act
            var code = _registrationCodeManager.GenerateCodeFormat("HOST-1", "MAIN-2", 6);

            // Assert
            var parts = code.Split('-');
            parts.Length.ShouldBe(5); // HOST, 1, MAIN, 2, RANDOM - more parts due to hyphens
            code.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public void GenerateCodeFormat_Should_Support_Variable_Random_Length()
        {
            // Act
            var code4 = _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN", 4);
            var code8 = _registrationCodeManager.GenerateCodeFormat("HOST", "MAIN", 8);

            // Assert
            var parts4 = code4.Split('-');
            var parts8 = code8.Split('-');

            parts4[2].Length.ShouldBe(4);
            parts8[2].Length.ShouldBe(8);
        }

        [Fact]
        public void GenerateCodeFormat_Should_Throw_When_Generated_Code_Exceeds_Length()
        {
            // Use very long codes that when combined exceed 50 chars
            var longTenantCode = "TENANT-WITH-VERY-LONG-NAME-EXCEEDING-LIMIT";
            var longUnitCode = "UNIT-WITH-VERY-LONG-NAME-ALSO-EXCEEDING";
            var randomLength = 10;

            // Act & Assert
            Should.Throw<BusinessException>(
                () => _registrationCodeManager.GenerateCodeFormat(longTenantCode, longUnitCode, randomLength))
                .Code.ShouldBe("RegistrationCode.CodeTooLong");
        }

        #endregion
    }
}
