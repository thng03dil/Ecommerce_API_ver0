using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.DTOs.Role;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class RoleServiceTests
{
    private readonly Mock<IRoleRepo> _roleRepo = new();
    private readonly Mock<IPermissionRepo> _permissionRepo = new();
    private readonly Mock<IUserRepo> _userRepo = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _sut = new RoleService(
            _roleRepo.Object,
            _permissionRepo.Object,
            _userRepo.Object,
            _cacheService.Object,
            _unitOfWork.Object);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenCacheHits_ShouldNotCallRepository()
    {
        var pagination = new PaginationDto { PageNumber = 1, PageSize = 10 };
        var cached = new PagedResponse<RoleResponseDto>(
            new List<RoleResponseDto> { new() { Id = 1, Name = "Admin" } },
            1,
            10,
            1);

        _cacheService.Setup(x => x.GetVersionAsync(CacheKeyGenerator.RoleVersionKey())).ReturnsAsync("3");
        _cacheService.Setup(x => x.GetAsync<PagedResponse<RoleResponseDto>>(It.IsAny<string>())).ReturnsAsync(cached);

        var result = await _sut.GetAllAsync(pagination);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cached);
        _roleRepo.Verify(x => x.GetAllAsync(It.IsAny<PaginationDto>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheMiss_ShouldLoadFromRepoAndSetCache()
    {
        var pagination = new PaginationDto { PageNumber = 1, PageSize = 10 };
        var roles = new List<Role> { new() { Id = 1, Name = "Admin", RolePermissions = new List<RolePermission>() } };

        _cacheService.Setup(x => x.GetVersionAsync(CacheKeyGenerator.RoleVersionKey())).ReturnsAsync("1");
        _cacheService
            .Setup(x => x.GetAsync<PagedResponse<RoleResponseDto>>(It.IsAny<string>()))
            .ReturnsAsync((PagedResponse<RoleResponseDto>?)null);
        _roleRepo.Setup(x => x.GetAllAsync(pagination)).ReturnsAsync((roles, 1));

        var result = await _sut.GetAllAsync(pagination);

        result.Success.Should().BeTrue();
        result.Data!.Data.Should().ContainSingle(x => x.Name == "Admin");
        _roleRepo.Verify(x => x.GetAllAsync(pagination), Times.Once);
        _cacheService.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResponse<RoleResponseDto>>(), It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenCacheHits_ShouldNotQueryDatabase()
    {
        var cached = new RoleWithPermissionsDto { Id = 2, Name = "Cached", Permissions = new List<PermissionResponseDto>() };
        _cacheService.Setup(x => x.GetAsync<RoleWithPermissionsDto>(It.IsAny<string>())).ReturnsAsync(cached);

        var result = await _sut.GetByIdAsync(2);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cached);
        _roleRepo.Verify(x => x.GetByIdWithPermissionsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_ShouldMapPermissionsAndSetCache()
    {
        var perm = new Permission { Id = 9, Name = "a.b", Entity = "a", Action = "b", Description = "" };
        var role = new Role
        {
            Id = 3,
            Name = "R",
            Description = "d",
            RolePermissions = new List<RolePermission>
            {
                new() { RoleId = 3, PermissionId = 9, Permission = perm }
            }
        };

        _cacheService.Setup(x => x.GetAsync<RoleWithPermissionsDto>(It.IsAny<string>())).ReturnsAsync((RoleWithPermissionsDto?)null);
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(3)).ReturnsAsync(role);

        var result = await _sut.GetByIdAsync(3);

        result.Success.Should().BeTrue();
        result.Data!.Permissions.Should().ContainSingle(p => p.Id == 9);
        _cacheService.Verify(
            x => x.SetAsync(CacheKeyGenerator.Role(3), It.IsAny<RoleWithPermissionsDto>(), It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFoundException()
    {
        _cacheService.Setup(x => x.GetAsync<RoleWithPermissionsDto>(It.IsAny<string>())).ReturnsAsync((RoleWithPermissionsDto?)null);
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(99)).ReturnsAsync((Role?)null);

        var act = () => _sut.GetByIdAsync(99);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.ErrorCode.Should().Be("Role not found");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_ShouldThrowBusinessException()
    {
        var dto = new RoleCreateDto { Name = "ExistingRole" };
        _roleRepo.Setup(x => x.ExistsByNameAsync(dto.Name)).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(dto);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should().Be("Role name already exists");
    }

    [Fact]
    public async Task CreateAsync_WhenPermissionIdsInvalid_ShouldThrowBusinessException()
    {
        var dto = new RoleCreateDto { Name = "New", PermissionIds = new List<int> { 1, 2 } };
        _roleRepo.Setup(x => x.ExistsByNameAsync("New")).ReturnsAsync(false);
        _permissionRepo.Setup(x => x.AllIdsExistAsync(dto.PermissionIds)).ReturnsAsync(false);

        var act = () => _sut.CreateAsync(dto);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("One or more Permission IDs do not exist.");
    }

    [Fact]
    public async Task CreateAsync_WhenValidWithoutPermissions_ShouldIncrementRoleVersionOnly()
    {
        var dto = new RoleCreateDto { Name = "Editor", Description = "e" };
        _roleRepo.Setup(x => x.ExistsByNameAsync("Editor")).ReturnsAsync(false);
        _roleRepo
            .Setup(x => x.AddAsync(It.IsAny<Role>()))
            .Callback<Role>(r => r.Id = 5)
            .Returns(Task.CompletedTask);
        var created = new Role
        {
            Id = 5,
            Name = "Editor",
            Description = "e",
            RolePermissions = new List<RolePermission>()
        };
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(5)).ReturnsAsync(created);

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.PermissionVersionKey()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenValidWithPermissions_ShouldIncrementRoleAndPermissionVersions()
    {
        var dto = new RoleCreateDto { Name = "Mod", PermissionIds = new List<int> { 10 } };
        _roleRepo.Setup(x => x.ExistsByNameAsync("Mod")).ReturnsAsync(false);
        _permissionRepo.Setup(x => x.AllIdsExistAsync(dto.PermissionIds)).ReturnsAsync(true);
        _roleRepo
            .Setup(x => x.AddAsync(It.IsAny<Role>()))
            .Callback<Role>(r => r.Id = 6)
            .Returns(Task.CompletedTask);
        var created = new Role { Id = 6, Name = "Mod", RolePermissions = new List<RolePermission>() };
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(6)).ReturnsAsync(created);

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.PermissionVersionKey()), Times.Once);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenRoleMissing_ShouldThrowNotFoundException()
    {
        _roleRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Role?)null);

        var act = () => _sut.UpdateAsync(1, new RoleUpdateDto { Name = "X", Description = "d" });

        (await act.Should().ThrowAsync<NotFoundException>()).Which.ErrorCode.Should().Be("Role not found");
    }

    [Fact]
    public async Task UpdateAsync_WhenNewNameConflicts_ShouldThrowBusinessException()
    {
        var role = new Role { Id = 1, Name = "Old" };
        _roleRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(role);
        _roleRepo.Setup(x => x.ExistsByNameAsync("Taken")).ReturnsAsync(true);

        var act = () => _sut.UpdateAsync(1, new RoleUpdateDto { Name = "Taken", Description = "d" });

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should().Be("New role name already exists");
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_ShouldRemoveRoleCacheAndBumpVersion()
    {
        var role = new Role { Id = 7, Name = "Old", Description = "x" };
        var updated = new Role { Id = 7, Name = "New", Description = "y", RolePermissions = new List<RolePermission>() };
        _roleRepo.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(role);
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(7)).ReturnsAsync(updated);

        var result = await _sut.UpdateAsync(7, new RoleUpdateDto { Name = "New", Description = "y" });

        result.Success.Should().BeTrue();
        _roleRepo.Verify(x => x.UpdateAsync(role), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.Role(7)), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenRenamingBuiltInAdmin_ShouldThrowBusinessException()
    {
        var role = new Role { Id = 1, Name = "Admin", Description = "x" };
        _roleRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(role);

        var act = () => _sut.UpdateAsync(1, new RoleUpdateDto { Name = "SuperAdmin", Description = "x" });

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("Cannot rename the built-in Admin or User role.");
    }

    #endregion

    #region AssignPermissionsAsync

    [Fact]
    public async Task AssignPermissionsAsync_WhenRoleMissing_ShouldThrowNotFoundException()
    {
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(1)).ReturnsAsync((Role?)null);

        var act = () => _sut.AssignPermissionsAsync(new AssignPermissionsDto { RoleId = 1, PermissionIds = new List<int>() });

        (await act.Should().ThrowAsync<NotFoundException>()).Which.ErrorCode.Should().Be("Role not found");
    }

    [Fact]
    public async Task AssignPermissionsAsync_WhenPermissionIdsInvalid_ShouldThrowBusinessException()
    {
        var role = new Role { Id = 1, RolePermissions = new List<RolePermission>() };
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(1)).ReturnsAsync(role);
        _permissionRepo.Setup(x => x.AllIdsExistAsync(It.IsAny<List<int>>())).ReturnsAsync(false);

        var act = () => _sut.AssignPermissionsAsync(new AssignPermissionsDto { RoleId = 1, PermissionIds = new List<int> { 99 } });

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("One or more Permission IDs do not exist.");
    }

    [Fact]
    public async Task AssignPermissionsAsync_WhenValid_ShouldUpdateCachesWithoutSessionInvalidation()
    {
        var roleId = 1;
        var permissionIds = new List<int> { 10, 11 };
        var role = new Role { Id = roleId, Name = "Editor", RolePermissions = new List<RolePermission>() };

        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(roleId)).ReturnsAsync(role);
        _permissionRepo.Setup(x => x.AllIdsExistAsync(permissionIds)).ReturnsAsync(true);

        var result = await _sut.AssignPermissionsAsync(new AssignPermissionsDto { RoleId = roleId, PermissionIds = permissionIds });

        result.Success.Should().BeTrue();
        _roleRepo.Verify(x => x.UpdateAsync(role), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.Role(roleId)), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.PermissionVersionKey()), Times.Once);
        _userRepo.Verify(x => x.GetActiveUserIdsByRoleIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignPermissionsAsync_WhenUserRoleAndExtraPermission_ShouldThrowBusinessException()
    {
        var role = new Role { Id = 2, Name = "User", RolePermissions = new List<RolePermission>() };
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(2)).ReturnsAsync(role);
        _permissionRepo.Setup(x => x.AllIdsExistAsync(It.IsAny<List<int>>())).ReturnsAsync(true);
        _permissionRepo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync(new Permission { Id = 99, Name = "role.delete" });

        var act = () => _sut.AssignPermissionsAsync(new AssignPermissionsDto { RoleId = 2, PermissionIds = new List<int> { 99 } });

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("The User role may only be assigned product.read and category.read permissions.");
    }

    [Fact]
    public async Task AssignPermissionsAsync_WhenUserRoleAndAllowedPermissions_ShouldSucceed()
    {
        var role = new Role { Id = 2, Name = "User", RolePermissions = new List<RolePermission>() };
        var ids = new List<int> { 1, 5 };
        _roleRepo.Setup(x => x.GetByIdWithPermissionsAsync(2)).ReturnsAsync(role);
        _permissionRepo.Setup(x => x.AllIdsExistAsync(ids)).ReturnsAsync(true);
        _permissionRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Permission { Id = 1, Name = "product.read" });
        _permissionRepo.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(new Permission { Id = 5, Name = "category.read" });

        var result = await _sut.AssignPermissionsAsync(new AssignPermissionsDto { RoleId = 2, PermissionIds = ids });

        result.Success.Should().BeTrue();
        _roleRepo.Verify(x => x.UpdateAsync(role), Times.Once);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenRoleMissing_ShouldThrowNotFoundException()
    {
        _roleRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Role?)null);

        var act = () => _sut.DeleteAsync(1);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.ErrorCode.Should().Be("Role not found");
    }

    [Fact]
    public async Task DeleteAsync_WhenBuiltInAdminName_ShouldThrowBusinessException()
    {
        var role = new Role { Id = 3, Name = "Admin" };
        _roleRepo.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(role);

        var act = () => _sut.DeleteAsync(3);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("Cannot delete the built-in Admin or User role.");
    }

    [Fact]
    public async Task DeleteAsync_WhenBuiltInUserName_ShouldThrowBeforeDefaultRoleLookup()
    {
        var roleId = 2;
        var role = new Role { Id = roleId, Name = "User" };
        _roleRepo.Setup(x => x.GetByIdAsync(roleId)).ReturnsAsync(role);

        var act = () => _sut.DeleteAsync(roleId);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("Cannot delete the built-in Admin or User role.");
        _roleRepo.Verify(x => x.GetByNameRoleAsync("User"), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_ShouldRunTransactionAndClearUserCachesWithoutSessionInvalidation()
    {
        var roleId = 5;
        var defaultRoleId = 1;
        var affectedUsers = new List<int> { 500, 501 };

        var roleToDelete = new Role { Id = roleId, Name = "Temporary" };
        var defaultRole = new Role { Id = defaultRoleId, Name = "User" };

        _roleRepo.Setup(x => x.GetByIdAsync(roleId)).ReturnsAsync(roleToDelete);
        _roleRepo.Setup(x => x.GetByNameRoleAsync("User")).ReturnsAsync(defaultRole);
        _userRepo.Setup(x => x.ReassignUsersToRoleAsync(roleId, defaultRoleId)).ReturnsAsync(affectedUsers);

        _unitOfWork
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<IReadOnlyList<int>>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<IReadOnlyList<int>>>, CancellationToken>((fn, _) => fn());

        var result = await _sut.DeleteAsync(roleId);

        result.Success.Should().BeTrue();
        roleToDelete.IsDeleted.Should().BeTrue();
        _unitOfWork.Verify(
            x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<IReadOnlyList<int>>>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.Role(roleId)), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.PermissionVersionKey()), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.User(500)), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.User(501)), Times.Once);
    }

    #endregion
}
