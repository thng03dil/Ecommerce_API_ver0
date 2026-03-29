using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Permission;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class PermissionServiceTests
{
    private readonly Mock<IPermissionRepo> _permissionRepo = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        _sut = new PermissionService(
            _permissionRepo.Object,
            _cacheService.Object);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenCacheHits_ShouldNotCallRepository()
    {
        var pagination = new PaginationDto { PageNumber = 1, PageSize = 10 };
        var cached = new PagedResponse<PermissionResponseDto>(
            new List<PermissionResponseDto> { new() { Id = 1, Name = "a.b", Entity = "a", Action = "b" } },
            1,
            10,
            1);

        _cacheService.Setup(x => x.GetVersionAsync(CacheKeyGenerator.PermissionVersionKey())).ReturnsAsync("2");
        _cacheService.Setup(x => x.GetAsync<PagedResponse<PermissionResponseDto>>(It.IsAny<string>())).ReturnsAsync(cached);

        var result = await _sut.GetAllAsync(pagination);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cached);
        _permissionRepo.Verify(x => x.GetAllAsync(It.IsAny<PaginationDto>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheMiss_ShouldLoadFromRepoAndSetCache()
    {
        var pagination = new PaginationDto { PageNumber = 2, PageSize = 5 };
        var perm = new Permission
        {
            Id = 9,
            Name = "x.y",
            Entity = "x",
            Action = "y",
            Description = "",
            CreatedAt = DateTime.UtcNow
        };

        _cacheService.Setup(x => x.GetVersionAsync(CacheKeyGenerator.PermissionVersionKey())).ReturnsAsync("1");
        _cacheService
            .Setup(x => x.GetAsync<PagedResponse<PermissionResponseDto>>(It.IsAny<string>()))
            .ReturnsAsync((PagedResponse<PermissionResponseDto>?)null);
        _permissionRepo.Setup(x => x.GetAllAsync(pagination)).ReturnsAsync(([perm], 1));

        var result = await _sut.GetAllAsync(pagination);

        result.Success.Should().BeTrue();
        result.Data!.Data.Should().ContainSingle(x => x.Id == 9 && x.Entity == "x");
        _permissionRepo.Verify(x => x.GetAllAsync(pagination), Times.Once);
        _cacheService.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResponse<PermissionResponseDto>>(), It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenCacheHits_ShouldNotQueryDatabase()
    {
        var cached = new PermissionResponseDto { Id = 3, Name = "e.a", Entity = "e", Action = "a" };
        _cacheService.Setup(x => x.GetAsync<PermissionResponseDto>(It.IsAny<string>())).ReturnsAsync(cached);

        var result = await _sut.GetByIdAsync(3);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cached);
        _permissionRepo.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_ShouldLoadSetCacheAndReturn()
    {
        var entity = new Permission
        {
            Id = 4,
            Name = "e.a",
            Entity = "e",
            Action = "a",
            Description = "d",
            CreatedAt = DateTime.UtcNow
        };
        _cacheService.Setup(x => x.GetAsync<PermissionResponseDto>(It.IsAny<string>())).ReturnsAsync((PermissionResponseDto?)null);
        _permissionRepo.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(4);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(4);
        _permissionRepo.Verify(x => x.GetByIdAsync(4), Times.Once);
        _cacheService.Verify(
            x => x.SetAsync(CacheKeyGenerator.Permission(4), It.Is<PermissionResponseDto>(p => p.Entity == "e"), It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFoundException()
    {
        _cacheService.Setup(x => x.GetAsync<PermissionResponseDto>(It.IsAny<string>())).ReturnsAsync((PermissionResponseDto?)null);
        _permissionRepo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Permission?)null);

        var act = () => _sut.GetByIdAsync(99);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.ErrorCode.Should().Be("Permission not found");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WhenEntityOrActionEmpty_ShouldThrowBusinessException()
    {
        var act = () => _sut.CreateAsync(new PermissionCreateDto { Entity = " ", Action = "read" });

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should().Be("Entity and Action are required.");
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateEntityAction_ShouldThrowBusinessException()
    {
        _permissionRepo.Setup(x => x.ExistsByEntityActionAsync("prod", "view", null)).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(new PermissionCreateDto { Entity = "prod", Action = "view" });

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("Permission already exists (Entity + Action must be unique).");
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ShouldPersistAndBumpPermissionVersionOnly()
    {
        _permissionRepo.Setup(x => x.ExistsByEntityActionAsync("cat", "list", null)).ReturnsAsync(false);
        _permissionRepo
            .Setup(x => x.AddAsync(It.IsAny<Permission>()))
            .Callback<Permission>(p => p.Id = 50)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new PermissionCreateDto { Entity = "cat", Action = "list", Description = "d" });

        result.Success.Should().BeTrue();
        result.Data!.Entity.Should().Be("cat");
        _permissionRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _permissionRepo.Verify(x => x.AddRolePermissionAsync(It.IsAny<RolePermission>()), Times.Never);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.PermissionVersionKey()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Never);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenPermissionMissing_ShouldThrowNotFoundException()
    {
        _permissionRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Permission?)null);

        var act = () => _sut.UpdateAsync(1, new PermissionUpdateDto { Entity = "a", Action = "b" });

        (await act.Should().ThrowAsync<NotFoundException>()).Which.ErrorCode.Should().Be("Permission not found");
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityOrActionEmpty_ShouldThrowBusinessException()
    {
        var p = new Permission { Id = 1, Entity = "a", Action = "b", Name = "a.b", CreatedAt = DateTime.UtcNow };
        _permissionRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(p);

        var act = () => _sut.UpdateAsync(1, new PermissionUpdateDto { Entity = "", Action = "z" });

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should().Be("Entity and Action are required.");
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_ShouldRemoveItemCacheAndBumpVersion()
    {
        var p = new Permission { Id = 2, Entity = "a", Action = "b", Name = "a.b", CreatedAt = DateTime.UtcNow };
        _permissionRepo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(p);
        _permissionRepo.Setup(x => x.ExistsByEntityActionAsync("c", "d", 2)).ReturnsAsync(false);

        var result = await _sut.UpdateAsync(2, new PermissionUpdateDto { Entity = "c", Action = "d", Description = "x" });

        result.Success.Should().BeTrue();
        p.Entity.Should().Be("c");
        p.Name.Should().Be("c.d");
        _permissionRepo.Verify(x => x.UpdateAsync(p), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.Permission(2)), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.PermissionVersionKey()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Once);
        _cacheService.Verify(x => x.RemoveByPrefixAsync(CacheKeyGenerator.RolePermissionCachePrefix()), Times.Once);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenPermissionMissing_ShouldThrowNotFoundException()
    {
        _permissionRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Permission?)null);

        var act = () => _sut.DeleteAsync(1);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.ErrorCode.Should().Be("Permission not found");
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_ShouldRemoveRoleLinksSoftDeleteAndBumpCaches()
    {
        var p = new Permission { Id = 8, Entity = "a", Action = "b", Name = "a.b", CreatedAt = DateTime.UtcNow };
        _permissionRepo.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(p);

        var result = await _sut.DeleteAsync(8);

        result.Success.Should().BeTrue();
        p.IsDeleted.Should().BeTrue();
        _permissionRepo.Verify(x => x.HardDeleteRolePermissionsByPermissionIdAsync(8), Times.Once);
        _permissionRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.Permission(8)), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.PermissionVersionKey()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.RoleVersionKey()), Times.Once);
        _cacheService.Verify(x => x.RemoveByPrefixAsync(CacheKeyGenerator.RolePermissionCachePrefix()), Times.Once);
    }

    #endregion
}
