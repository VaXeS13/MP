using System;
using MP.Domain.OrganizationalUnits;
using Shouldly;
using Xunit;

namespace MP.Domain.Tests.OrganizationalUnits
{
    public class UserOrganizationalUnitTests : MPDomainTestBase<MPDomainTestModule>
    {
        private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid TestUnitId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        private static readonly Guid TestRoleId = Guid.Parse("00000000-0000-0000-0000-000000000004");

        [Fact]
        public void Should_Create_UserOrganizationalUnit_Without_Role()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var userUnit = new UserOrganizationalUnit(id, TestUserId, TestUnitId, null, TestTenantId);

            // Assert
            userUnit.Id.ShouldBe(id);
            userUnit.UserId.ShouldBe(TestUserId);
            userUnit.OrganizationalUnitId.ShouldBe(TestUnitId);
            userUnit.RoleId.ShouldBeNull();
            userUnit.TenantId.ShouldBe(TestTenantId);
            userUnit.IsActive.ShouldBeTrue();
            userUnit.AssignedAt.ShouldNotBe(default(DateTime));
        }

        [Fact]
        public void Should_Create_UserOrganizationalUnit_With_Role()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var userUnit = new UserOrganizationalUnit(id, TestUserId, TestUnitId, TestRoleId, TestTenantId);

            // Assert
            userUnit.Id.ShouldBe(id);
            userUnit.UserId.ShouldBe(TestUserId);
            userUnit.OrganizationalUnitId.ShouldBe(TestUnitId);
            userUnit.RoleId.ShouldBe(TestRoleId);
            userUnit.TenantId.ShouldBe(TestTenantId);
            userUnit.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Should_Create_UserOrganizationalUnit_For_Host_Tenant()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var userUnit = new UserOrganizationalUnit(id, TestUserId, TestUnitId, null, null);

            // Assert
            userUnit.TenantId.ShouldBeNull();
            userUnit.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Should_Update_Role()
        {
            // Arrange
            var userUnit = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, null, TestTenantId);
            var newRoleId = Guid.NewGuid();

            // Act
            userUnit.UpdateRole(newRoleId);

            // Assert
            userUnit.RoleId.ShouldBe(newRoleId);
        }

        [Fact]
        public void Should_Clear_Role()
        {
            // Arrange
            var userUnit = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, TestRoleId, TestTenantId);

            // Act
            userUnit.UpdateRole(null);

            // Assert
            userUnit.RoleId.ShouldBeNull();
        }

        [Fact]
        public void Should_Deactivate_User_Unit()
        {
            // Arrange
            var userUnit = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, null, TestTenantId);
            userUnit.IsActive.ShouldBeTrue();

            // Act
            userUnit.Deactivate();

            // Assert
            userUnit.IsActive.ShouldBeFalse();
        }

        [Fact]
        public void Should_Activate_User_Unit()
        {
            // Arrange
            var userUnit = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, null, TestTenantId);
            userUnit.Deactivate();
            userUnit.IsActive.ShouldBeFalse();

            // Act
            userUnit.Activate();

            // Assert
            userUnit.IsActive.ShouldBeTrue();
        }

        [Fact]
        public void Should_Set_AssignedAt_When_Created()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow;

            // Act
            var userUnit = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, null, TestTenantId);

            var afterCreation = DateTime.UtcNow;

            // Assert
            userUnit.AssignedAt.ShouldBeGreaterThanOrEqualTo(beforeCreation);
            userUnit.AssignedAt.ShouldBeLessThanOrEqualTo(afterCreation);
        }

        [Fact]
        public void Should_Support_Multiple_Units_Per_User()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var unitId1 = Guid.NewGuid();
            var unitId2 = Guid.NewGuid();

            // Act
            var userUnit1 = new UserOrganizationalUnit(Guid.NewGuid(), userId, unitId1, null, TestTenantId);
            var userUnit2 = new UserOrganizationalUnit(Guid.NewGuid(), userId, unitId2, null, TestTenantId);

            // Assert
            userUnit1.UserId.ShouldBe(userId);
            userUnit2.UserId.ShouldBe(userId);
            userUnit1.OrganizationalUnitId.ShouldNotBe(userUnit2.OrganizationalUnitId);
        }
    }
}
