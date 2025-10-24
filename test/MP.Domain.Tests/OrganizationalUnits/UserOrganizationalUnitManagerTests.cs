using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MP.Domain.OrganizationalUnits;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace MP.Domain.Tests.OrganizationalUnits
{
    /// <summary>
    /// Unit tests for UserOrganizationalUnitManager domain service.
    /// Tests user assignment, role management, and access control.
    /// </summary>
    public class UserOrganizationalUnitManagerTests
    {
        private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        private static readonly Guid TestUser2Id = Guid.Parse("00000000-0000-0000-0000-000000000005");
        private static readonly Guid TestUnitId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        private static readonly Guid TestRoleId = Guid.Parse("00000000-0000-0000-0000-000000000004");
        private static readonly Guid TestRole2Id = Guid.Parse("00000000-0000-0000-0000-000000000006");

        private readonly IUserOrganizationalUnitRepository _userUnitRepository;
        private readonly IOrganizationalUnitRepository _unitRepository;
        private readonly IOrganizationalUnitRegistrationCodeRepository _codeRepository;
        private readonly IGuidGenerator _guidGenerator;
        private readonly ICurrentTenant _currentTenant;
        private readonly IRepository<IdentityRole, Guid> _roleRepository;
        private readonly RegistrationCodeManager _codeManager;
        private readonly UserOrganizationalUnitManager _manager;

        public UserOrganizationalUnitManagerTests()
        {
            _userUnitRepository = Substitute.For<IUserOrganizationalUnitRepository>();
            _unitRepository = Substitute.For<IOrganizationalUnitRepository>();
            _codeRepository = Substitute.For<IOrganizationalUnitRegistrationCodeRepository>();
            _guidGenerator = Substitute.For<IGuidGenerator>();
            _currentTenant = Substitute.For<ICurrentTenant>();
            _roleRepository = Substitute.For<IRepository<IdentityRole, Guid>>();

            // Configure mocks
            _currentTenant.Id.Returns(TestTenantId);
            _guidGenerator.Create().Returns(x => Guid.NewGuid());

            // Create real RegistrationCodeManager with mocked dependencies
            _codeManager = new RegistrationCodeManager(
                _codeRepository,
                _unitRepository,
                _guidGenerator,
                _currentTenant);

            _manager = new UserOrganizationalUnitManager(
                _userUnitRepository,
                _unitRepository,
                _codeManager,
                _guidGenerator,
                _currentTenant,
                _roleRepository);
        }

        #region AssignUserToUnitAsync Tests

        [Fact]
        public async Task AssignUserToUnitAsync_Should_Create_Assignment()
        {
            // Arrange
            var unit = new OrganizationalUnit(TestUnitId, "Test Unit", "TEST", TestTenantId);
            _unitRepository.GetAsync(TestUnitId).Returns(unit);
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(false);
            _userUnitRepository.InsertAsync(Arg.Any<UserOrganizationalUnit>()).Returns(Task.CompletedTask);

            // Act
            var result = await _manager.AssignUserToUnitAsync(TestUserId, TestUnitId);

            // Assert
            result.ShouldNotBeNull();
            result.UserId.ShouldBe(TestUserId);
            result.OrganizationalUnitId.ShouldBe(TestUnitId);
            result.IsActive.ShouldBeTrue();
            await _userUnitRepository.Received(1).InsertAsync(Arg.Any<UserOrganizationalUnit>());
        }

        [Fact]
        public async Task AssignUserToUnitAsync_Should_Create_Assignment_With_Role()
        {
            // Arrange
            var unit = new OrganizationalUnit(TestUnitId, "Test Unit", "TEST", TestTenantId);
            var testRole = new IdentityRole(TestRoleId, "Admin", TestTenantId);

            _unitRepository.GetAsync(TestUnitId).Returns(unit);
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(false);
            _roleRepository.FindAsync(TestRoleId).Returns(Task.FromResult<IdentityRole?>(testRole));
            _userUnitRepository.InsertAsync(Arg.Any<UserOrganizationalUnit>()).Returns(Task.CompletedTask);

            // Act
            var result = await _manager.AssignUserToUnitAsync(TestUserId, TestUnitId, TestRoleId);

            // Assert
            result.ShouldNotBeNull();
            result.RoleId.ShouldBe(TestRoleId);
        }

        [Fact]
        public async Task AssignUserToUnitAsync_Should_Throw_When_Unit_Not_Found()
        {
            // Arrange
            _unitRepository.GetAsync(TestUnitId).Returns((OrganizationalUnit?)null);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.AssignUserToUnitAsync(TestUserId, TestUnitId));

            exception.Code.ShouldBe("UserOrganizationalUnit.UnitNotFound");
        }

        [Fact]
        public async Task AssignUserToUnitAsync_Should_Throw_When_Already_Assigned()
        {
            // Arrange
            var unit = new OrganizationalUnit(TestUnitId, "Test Unit", "TEST", TestTenantId);
            _unitRepository.GetAsync(TestUnitId).Returns(unit);
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(true);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.AssignUserToUnitAsync(TestUserId, TestUnitId));

            exception.Code.ShouldBe("UserOrganizationalUnit.AlreadyAssigned");
        }

        [Fact]
        public async Task AssignUserToUnitAsync_Should_Throw_When_Role_Not_Found()
        {
            // Arrange
            var unit = new OrganizationalUnit(TestUnitId, "Test Unit", "TEST", TestTenantId);
            _unitRepository.GetAsync(TestUnitId).Returns(unit);
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(false);
            _roleRepository.FindAsync(TestRoleId).Returns(Task.FromResult<IdentityRole?>(null));

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.AssignUserToUnitAsync(TestUserId, TestUnitId, TestRoleId));

            exception.Code.ShouldBe("UserOrganizationalUnit.RoleNotFound");
        }

        #endregion

        #region JoinUnitWithCodeAsync Tests

        [Fact]
        public async Task JoinUnitWithCodeAsync_Should_Create_Assignment_From_Code()
        {
            // Arrange
            var unit = new OrganizationalUnit(TestUnitId, "Test Unit", "TEST", TestTenantId);
            var code = new OrganizationalUnitRegistrationCode(
                Guid.NewGuid(),
                TestUnitId,
                "TEST-MAIN-ABC123",
                TestTenantId);
            code.SetRoleId(TestRoleId);

            _codeManager.ValidateCodeAsync(TestTenantId, "TEST-MAIN-ABC123").Returns(code);
            _unitRepository.GetAsync(TestUnitId).Returns(unit);
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(false);
            _roleRepository.FindAsync(TestRoleId).Returns(Task.FromResult<IdentityRole?>(new IdentityRole(TestRoleId, "Admin", TestTenantId)));
            _userUnitRepository.InsertAsync(Arg.Any<UserOrganizationalUnit>()).Returns(Task.CompletedTask);
            _codeManager.UseCodeAsync(code.Id).Returns(code);

            // Act
            var result = await _manager.JoinUnitWithCodeAsync(TestUserId, "TEST-MAIN-ABC123");

            // Assert
            result.ShouldNotBeNull();
            result.UserId.ShouldBe(TestUserId);
            result.OrganizationalUnitId.ShouldBe(TestUnitId);
            result.RoleId.ShouldBe(TestRoleId);
            await _codeManager.Received(1).UseCodeAsync(code.Id);
        }

        [Fact]
        public async Task JoinUnitWithCodeAsync_Should_Throw_When_Code_Empty()
        {
            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.JoinUnitWithCodeAsync(TestUserId, ""));

            exception.Code.ShouldBe("UserOrganizationalUnit.CodeRequired");
        }

        [Fact]
        public async Task JoinUnitWithCodeAsync_Should_Throw_When_Code_Invalid()
        {
            // Arrange
            _codeManager.ValidateCodeAsync(TestTenantId, "INVALID").Returns(
                Task.FromException<OrganizationalUnitRegistrationCode>(
                    new BusinessException("RegistrationCode.NotFound", "Code not found")));

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.JoinUnitWithCodeAsync(TestUserId, "INVALID"));
        }

        #endregion

        #region RemoveUserFromUnitAsync Tests

        [Fact]
        public async Task RemoveUserFromUnitAsync_Should_Remove_Non_Admin()
        {
            // Arrange
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(true);
            _userUnitRepository.GetOrganizationalUnitUserIdsAsync(TestTenantId, TestUnitId)
                .Returns(new List<Guid> { TestUserId, TestUser2Id });

            var userMembership = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, null, TestTenantId);
            var admin2Membership = new UserOrganizationalUnit(Guid.NewGuid(), TestUser2Id, TestUnitId, TestRoleId, TestTenantId);

            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { userMembership });
            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUser2Id, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { admin2Membership });

            _userUnitRepository.RemoveUserFromUnitAsync(TestTenantId, TestUserId, TestUnitId).Returns(Task.CompletedTask);

            // Act
            await _manager.RemoveUserFromUnitAsync(TestUserId, TestUnitId);

            // Assert
            await _userUnitRepository.Received(1).RemoveUserFromUnitAsync(TestTenantId, TestUserId, TestUnitId);
        }

        [Fact]
        public async Task RemoveUserFromUnitAsync_Should_Throw_When_User_Not_Member()
        {
            // Arrange
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(false);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.RemoveUserFromUnitAsync(TestUserId, TestUnitId));

            exception.Code.ShouldBe("UserOrganizationalUnit.NotMember");
        }

        [Fact]
        public async Task RemoveUserFromUnitAsync_Should_Throw_When_Last_Admin()
        {
            // Arrange
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(true);
            _userUnitRepository.GetOrganizationalUnitUserIdsAsync(TestTenantId, TestUnitId)
                .Returns(new List<Guid> { TestUserId }); // Only one member

            var userMembership = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, TestRoleId, TestTenantId);
            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { userMembership });

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.RemoveUserFromUnitAsync(TestUserId, TestUnitId));

            exception.Code.ShouldBe("UserOrganizationalUnit.CannotRemoveLastAdmin");
        }

        #endregion

        #region UpdateUserRoleInUnitAsync Tests

        [Fact]
        public async Task UpdateUserRoleInUnitAsync_Should_Update_Role()
        {
            // Arrange
            var newRole = new IdentityRole(TestRole2Id, "Manager", TestTenantId);
            var membership = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, TestRoleId, TestTenantId);

            _roleRepository.FindAsync(TestRole2Id).Returns(Task.FromResult<IdentityRole?>(newRole));
            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { membership });
            _userUnitRepository.UpdateAsync(Arg.Any<UserOrganizationalUnit>()).Returns(x => Task.FromResult((UserOrganizationalUnit)x[0]));

            // Act
            var result = await _manager.UpdateUserRoleInUnitAsync(TestUserId, TestUnitId, TestRole2Id);

            // Assert
            result.RoleId.ShouldBe(TestRole2Id);
            await _userUnitRepository.Received(1).UpdateAsync(Arg.Any<UserOrganizationalUnit>());
        }

        [Fact]
        public async Task UpdateUserRoleInUnitAsync_Should_Clear_Role()
        {
            // Arrange
            var membership = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, TestRoleId, TestTenantId);

            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { membership });
            _userUnitRepository.UpdateAsync(Arg.Any<UserOrganizationalUnit>()).Returns(x => Task.FromResult((UserOrganizationalUnit)x[0]));

            // Act
            var result = await _manager.UpdateUserRoleInUnitAsync(TestUserId, TestUnitId, null);

            // Assert
            result.RoleId.ShouldBeNull();
        }

        [Fact]
        public async Task UpdateUserRoleInUnitAsync_Should_Throw_When_User_Not_Member()
        {
            // Arrange
            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit>());

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.UpdateUserRoleInUnitAsync(TestUserId, TestUnitId, TestRole2Id));

            exception.Code.ShouldBe("UserOrganizationalUnit.NotMember");
        }

        [Fact]
        public async Task UpdateUserRoleInUnitAsync_Should_Throw_When_Role_Not_Found()
        {
            // Arrange
            var membership = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, null, TestTenantId);

            _roleRepository.FindAsync(TestRole2Id).Returns((IdentityRole?)null);
            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { membership });

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.UpdateUserRoleInUnitAsync(TestUserId, TestUnitId, TestRole2Id));

            exception.Code.ShouldBe("UserOrganizationalUnit.RoleNotFound");
        }

        #endregion

        #region DeactivateUserInUnitAsync Tests

        [Fact]
        public async Task DeactivateUserInUnitAsync_Should_Deactivate_Non_Admin()
        {
            // Arrange
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(true);
            _userUnitRepository.GetOrganizationalUnitUserIdsAsync(TestTenantId, TestUnitId)
                .Returns(new List<Guid> { TestUserId, TestUser2Id });

            var userMembership = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, null, TestTenantId);
            var adminMembership = new UserOrganizationalUnit(Guid.NewGuid(), TestUser2Id, TestUnitId, TestRoleId, TestTenantId);

            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { userMembership });
            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUser2Id, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { adminMembership });
            _userUnitRepository.UpdateAsync(Arg.Any<UserOrganizationalUnit>()).Returns(x => Task.FromResult((UserOrganizationalUnit)x[0]));

            // Act
            await _manager.DeactivateUserInUnitAsync(TestUserId, TestUnitId);

            // Assert
            await _userUnitRepository.Received(1).UpdateAsync(Arg.Any<UserOrganizationalUnit>());
        }

        [Fact]
        public async Task DeactivateUserInUnitAsync_Should_Throw_When_User_Not_Member()
        {
            // Arrange
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(false);

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.DeactivateUserInUnitAsync(TestUserId, TestUnitId));

            exception.Code.ShouldBe("UserOrganizationalUnit.NotMember");
        }

        [Fact]
        public async Task DeactivateUserInUnitAsync_Should_Throw_When_Last_Admin()
        {
            // Arrange
            _userUnitRepository.IsMemberAsync(TestTenantId, TestUserId, TestUnitId).Returns(true);
            _userUnitRepository.GetOrganizationalUnitUserIdsAsync(TestTenantId, TestUnitId)
                .Returns(new List<Guid> { TestUserId }); // Only one member

            var userMembership = new UserOrganizationalUnit(Guid.NewGuid(), TestUserId, TestUnitId, TestRoleId, TestTenantId);
            _userUnitRepository.GetUserMembershipsAsync(TestTenantId, TestUserId, maxResultCount: int.MaxValue)
                .Returns(new List<UserOrganizationalUnit> { userMembership });

            // Act & Assert
            var exception = await Should.ThrowAsync<BusinessException>(
                () => _manager.DeactivateUserInUnitAsync(TestUserId, TestUnitId));

            exception.Code.ShouldBe("UserOrganizationalUnit.CannotDeactivateLastAdmin");
        }

        #endregion

        #region GetUserUnitsAsync Tests

        [Fact]
        public async Task GetUserUnitsAsync_Should_Return_User_Units()
        {
            // Arrange
            var unit1Id = Guid.NewGuid();
            var unit2Id = Guid.NewGuid();

            var unit1 = new OrganizationalUnit(unit1Id, "Unit 1", "UNIT1", TestTenantId);
            var unit2 = new OrganizationalUnit(unit2Id, "Unit 2", "UNIT2", TestTenantId);

            _userUnitRepository.GetUserOrganizationalUnitIdsAsync(TestTenantId, TestUserId)
                .Returns(new List<Guid> { unit1Id, unit2Id });
            _unitRepository.GetListAsync(tenantId: TestTenantId, isActive: true)
                .Returns(new List<OrganizationalUnit> { unit1, unit2 });

            // Act
            var result = await _manager.GetUserUnitsAsync(TestUserId, TestTenantId);

            // Assert
            result.Count.ShouldBe(2);
            result.ShouldContain(u => u.Id == unit1Id);
            result.ShouldContain(u => u.Id == unit2Id);
        }

        [Fact]
        public async Task GetUserUnitsAsync_Should_Return_Empty_When_No_Units()
        {
            // Arrange
            _userUnitRepository.GetUserOrganizationalUnitIdsAsync(TestTenantId, TestUserId)
                .Returns(new List<Guid>());

            // Act
            var result = await _manager.GetUserUnitsAsync(TestUserId, TestTenantId);

            // Assert
            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetUserUnitsAsync_Should_Include_Inactive_When_Requested()
        {
            // Arrange
            var unit1Id = Guid.NewGuid();
            var unit2Id = Guid.NewGuid();

            var unit1 = new OrganizationalUnit(unit1Id, "Unit 1", "UNIT1", TestTenantId);
            var unit2 = new OrganizationalUnit(unit2Id, "Unit 2", "UNIT2", TestTenantId);
            unit2.Deactivate();

            _userUnitRepository.GetUserOrganizationalUnitIdsAsync(TestTenantId, TestUserId)
                .Returns(new List<Guid> { unit1Id, unit2Id });
            _unitRepository.GetListAsync(tenantId: TestTenantId, isActive: null)
                .Returns(new List<OrganizationalUnit> { unit1, unit2 });

            // Act
            var result = await _manager.GetUserUnitsAsync(TestUserId, TestTenantId, includeInactive: true);

            // Assert
            result.Count.ShouldBe(2);
        }

        #endregion
    }
}
