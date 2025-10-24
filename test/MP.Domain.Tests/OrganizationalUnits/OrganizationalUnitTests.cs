using System;
using MP.Domain.OrganizationalUnits;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace MP.Domain.Tests.OrganizationalUnits
{
    public class OrganizationalUnitTests : MPDomainTestBase<MPDomainTestModule>
    {
        private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        [Fact]
        public void Should_Create_OrganizationalUnit_With_Valid_Data()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = "Main Unit";
            var code = "MAIN";

            // Act
            var unit = new OrganizationalUnit(id, name, code, TestTenantId);

            // Assert
            unit.Id.ShouldBe(id);
            unit.Name.ShouldBe(name);
            unit.Code.ShouldBe(code);
            unit.TenantId.ShouldBe(TestTenantId);
            unit.IsActive.ShouldBeTrue();
            unit.Address.ShouldBeNull();
            unit.City.ShouldBeNull();
            unit.PostalCode.ShouldBeNull();
            unit.Email.ShouldBeNull();
            unit.Phone.ShouldBeNull();
        }

        [Fact]
        public void Should_Throw_When_Name_Is_Null()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnit(Guid.NewGuid(), null!, "CODE", TestTenantId);
            }).Code.ShouldBe("OrganizationalUnit.NameRequired");
        }

        [Fact]
        public void Should_Throw_When_Name_Is_Empty()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnit(Guid.NewGuid(), "", "CODE", TestTenantId);
            }).Code.ShouldBe("OrganizationalUnit.NameRequired");
        }

        [Fact]
        public void Should_Throw_When_Name_Exceeds_Max_Length()
        {
            // Arrange
            var longName = new string('a', 201);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnit(Guid.NewGuid(), longName, "CODE", TestTenantId);
            }).Code.ShouldBe("OrganizationalUnit.NameTooLong");
        }

        [Fact]
        public void Should_Throw_When_Code_Is_Null()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnit(Guid.NewGuid(), "Name", null!, TestTenantId);
            }).Code.ShouldBe("OrganizationalUnit.CodeRequired");
        }

        [Fact]
        public void Should_Throw_When_Code_Is_Empty()
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnit(Guid.NewGuid(), "Name", "", TestTenantId);
            }).Code.ShouldBe("OrganizationalUnit.CodeRequired");
        }

        [Fact]
        public void Should_Throw_When_Code_Exceeds_Max_Length()
        {
            // Arrange
            var longCode = new string('A', 51);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnit(Guid.NewGuid(), "Name", longCode, TestTenantId);
            }).Code.ShouldBe("OrganizationalUnit.CodeTooLong");
        }

        [Theory]
        [InlineData("MAIN-UNIT")]
        [InlineData("ABC123")]
        [InlineData("unit-01")]
        public void Should_Accept_Valid_Code_Formats(string validCode)
        {
            // Act
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", validCode, TestTenantId);

            // Assert
            unit.Code.ShouldBe(validCode);
        }

        [Theory]
        [InlineData("MAIN UNIT")]
        [InlineData("MAIN@UNIT")]
        [InlineData("MAIN_UNIT")]
        [InlineData("MAIN.UNIT")]
        public void Should_Throw_When_Code_Contains_Invalid_Characters(string invalidCode)
        {
            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                new OrganizationalUnit(Guid.NewGuid(), "Name", invalidCode, TestTenantId);
            }).Code.ShouldBe("OrganizationalUnit.CodeInvalid");
        }

        [Fact]
        public void Should_Update_Contact_Info()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);

            // Act
            unit.UpdateContactInfo("123 Main St", "Warsaw", "00-001", "contact@example.com", "+48123456789");

            // Assert
            unit.Address.ShouldBe("123 Main St");
            unit.City.ShouldBe("Warsaw");
            unit.PostalCode.ShouldBe("00-001");
            unit.Email.ShouldBe("contact@example.com");
            unit.Phone.ShouldBe("+48123456789");
        }

        [Fact]
        public void Should_Throw_When_Address_Exceeds_Max_Length()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);
            var longAddress = new string('a', 301);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                unit.UpdateContactInfo(longAddress, null, null, null, null);
            }).Code.ShouldBe("OrganizationalUnit.AddressTooLong");
        }

        [Fact]
        public void Should_Throw_When_City_Exceeds_Max_Length()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);
            var longCity = new string('a', 101);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                unit.UpdateContactInfo(null, longCity, null, null, null);
            }).Code.ShouldBe("OrganizationalUnit.CityTooLong");
        }

        [Fact]
        public void Should_Throw_When_PostalCode_Exceeds_Max_Length()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);
            var longPostalCode = new string('1', 21);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                unit.UpdateContactInfo(null, null, longPostalCode, null, null);
            }).Code.ShouldBe("OrganizationalUnit.PostalCodeTooLong");
        }

        [Fact]
        public void Should_Throw_When_Email_Is_Invalid()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                unit.UpdateContactInfo(null, null, null, "invalid-email", null);
            }).Code.ShouldBe("OrganizationalUnit.EmailInvalid");
        }

        [Fact]
        public void Should_Throw_When_Email_Exceeds_Max_Length()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);
            var longEmail = new string('a', 240) + "@example.com";

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                unit.UpdateContactInfo(null, null, null, longEmail, null);
            }).Code.ShouldBe("OrganizationalUnit.EmailTooLong");
        }

        [Fact]
        public void Should_Throw_When_Phone_Exceeds_Max_Length()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);
            var longPhone = new string('1', 21);

            // Act & Assert
            Should.Throw<BusinessException>(() =>
            {
                unit.UpdateContactInfo(null, null, null, null, longPhone);
            }).Code.ShouldBe("OrganizationalUnit.PhoneTooLong");
        }

        [Fact]
        public void Should_SetName_Successfully()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Original Name", "CODE", TestTenantId);

            // Act
            unit.SetName("Updated Name");

            // Assert
            unit.Name.ShouldBe("Updated Name");
        }

        [Fact]
        public void Should_SetCode_Successfully()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "OLD-CODE", TestTenantId);

            // Act
            unit.SetCode("NEW-CODE");

            // Assert
            unit.Code.ShouldBe("NEW-CODE");
        }

        [Fact]
        public void Should_Activate_Unit()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);
            unit.Deactivate();
            unit.IsActive.ShouldBeFalse();

            // Act
            unit.Activate();

            // Assert
            unit.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Should_Deactivate_Unit()
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);
            unit.IsActive.ShouldBeTrue();

            // Act
            unit.Deactivate();

            // Assert
            unit.IsActive.ShouldBeFalse();
        }

        [Theory]
        [InlineData("valid.email@example.com")]
        [InlineData("test+tag@domain.co.uk")]
        [InlineData("user123@test-domain.com")]
        public void Should_Accept_Valid_Email_Formats(string validEmail)
        {
            // Arrange
            var unit = new OrganizationalUnit(Guid.NewGuid(), "Name", "CODE", TestTenantId);

            // Act
            unit.UpdateContactInfo(null, null, null, validEmail, null);

            // Assert
            unit.Email.ShouldBe(validEmail);
        }
    }
}
