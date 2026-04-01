using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepo> _categoryRepo = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categoryRepo.Object, _cacheService.Object);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WhenFirstCacheReadHits_ShouldReturnCachedDataWithoutRepo()
    {
        var filter = new CategoryFilterDto();
        var pagination = new PaginationDto { PageNumber = 1, PageSize = 10 };
        var cached = new PagedResponse<CategoryResponseDto>(new List<CategoryResponseDto>(), 1, 10, 0);

        _cacheService.Setup(x => x.GetVersionAsync(CacheKeyGenerator.CategoryVersionKey())).ReturnsAsync("1");
        _cacheService
            .Setup(x => x.GetAsync<PagedResponse<CategoryResponseDto>>(It.IsAny<string>()))
            .ReturnsAsync(cached);

        var result = await _sut.GetAllAsync(filter, pagination);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(cached);
        _categoryRepo.Verify(x => x.GetFilteredAsync(It.IsAny<CategoryFilterDto>(), It.IsAny<PaginationDto>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenCacheMiss_ShouldLoadFromRepoAndSetCache()
    {
        var filter = new CategoryFilterDto { Keyword = "x" };
        var pagination = new PaginationDto { PageNumber = 2, PageSize = 5 };
        var entity = new Category { Id = 10, Name = "N", Description = "D", Slug = "s", CreatedAt = DateTime.UtcNow };

        _cacheService.Setup(x => x.GetVersionAsync(CacheKeyGenerator.CategoryVersionKey())).ReturnsAsync("7");
        _cacheService
            .Setup(x => x.GetAsync<PagedResponse<CategoryResponseDto>>(It.IsAny<string>()))
            .ReturnsAsync((PagedResponse<CategoryResponseDto>?)null);
        _categoryRepo
            .Setup(x => x.GetFilteredAsync(filter, pagination))
            .ReturnsAsync(([entity], 1));

        var result = await _sut.GetAllAsync(filter, pagination);

        result.Success.Should().BeTrue();
        result.Data!.Data.Should().ContainSingle(x => x.Id == 10 && x.Name == "N");
        _categoryRepo.Verify(x => x.GetFilteredAsync(filter, pagination), Times.Once);
        _cacheService.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResponse<CategoryResponseDto>>(), It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenCacheHits_ShouldNotQueryDatabase()
    {
        var dto = new CategoryResponseDto { Id = 3, Name = "Cached" };
        _cacheService.Setup(x => x.GetAsync<CategoryResponseDto>(It.IsAny<string>())).ReturnsAsync(dto);

        var result = await _sut.GetByIdAsync(3);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(dto);
        _categoryRepo.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCacheMiss_ShouldLoadSetCacheAndReturn()
    {
        var category = new Category { Id = 4, Name = "Db", Slug = "db", CreatedAt = DateTime.UtcNow };
        _cacheService.Setup(x => x.GetAsync<CategoryResponseDto>(It.IsAny<string>())).ReturnsAsync((CategoryResponseDto?)null);
        _categoryRepo.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(category);

        var result = await _sut.GetByIdAsync(4);

        result.Success.Should().BeTrue();
        result.Data!.Name.Should().Be("Db");
        _categoryRepo.Verify(x => x.GetByIdAsync(4), Times.Once);
        _cacheService.Verify(
            x => x.SetAsync(CacheKeyGenerator.Category(4), It.Is<CategoryResponseDto>(c => c.Name == "Db"), It.IsAny<TimeSpan?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _cacheService.Setup(x => x.GetAsync<CategoryResponseDto>(It.IsAny<string>())).ReturnsAsync((CategoryResponseDto?)null);
        _categoryRepo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Category?)null);

        // Act
        var act = () => _sut.GetByIdAsync(99);

        // Assert
        var exception = await act.Should().ThrowAsync<NotFoundException>();

        // Sửa ở đây: Kiểm tra Message thay vì ErrorCode
        exception.Which.Message.Should().Be("Category not found");

        // Nếu muốn kiểm tra ErrorCode thì phải là:
        exception.Which.ErrorCode.Should().Be("NOT_FOUND");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidDto_ShouldPersistAndBumpCategoryListVersion()
    {
        var dto = new CategoryCreateDto { Name = "Tech", Description = "Desc", Slug = "tech" };

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(dto.Name);

        _categoryRepo.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == dto.Name)), Times.Once);
        _categoryRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }

    #endregion

    #region UpdateAsync
    [Fact]
    public async Task UpdateAsync_WhenCategoryMissing_ShouldThrowNotFoundException()
    {
        // Arrange
        _categoryRepo.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync((Category?)null);

        // Act
        var act = () => _sut.UpdateAsync(1, new CategoryUpdateDto { Name = "X", Slug = "x" });

        // Assert
        var exception = await act.Should().ThrowAsync<NotFoundException>();

        // Sửa thành Message
        exception.Which.Message.Should().Be("Category not found");
    }

    [Fact]
    public async Task UpdateAsync_WhenValid_ShouldUpdateRemoveItemCacheAndBumpVersion()
    {
        var id = 5;
        var existing = new Category { Id = id, Name = "Old Name", Slug = "old" };
        var updateDto = new CategoryUpdateDto { Name = "New Name", Description = "D", Slug = "new-slug" };

        _categoryRepo.Setup(x => x.GetByIdForUpdateAsync(id)).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(id, updateDto);

        result.Success.Should().BeTrue();
        existing.Name.Should().Be("New Name");
        existing.UpdatedAt.Should().NotBeNull();

        _categoryRepo.Verify(x => x.UpdateAsync(existing), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.Category(id)), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenCategoryNotFound_ShouldThrowNotFoundException()
    {
        _categoryRepo.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync((Category?)null);

        var act = () => _sut.DeleteAsync(1);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenHasLinkedProducts_ShouldThrowBadRequestException()
    {
        var category = new Category { Id = 1 };
        _categoryRepo.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(category);
        _categoryRepo.Setup(x => x.HasActiveProductsAsync(1)).ReturnsAsync(true);

        var act = () => _sut.DeleteAsync(1);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Cannot delete category with linked products");

        _categoryRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_ShouldSoftDeleteAndInvalidateCache()
    {
        var category = new Category { Id = 1, Name = "Food" };
        _categoryRepo.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(category);
        _categoryRepo.Setup(x => x.HasActiveProductsAsync(1)).ReturnsAsync(false);

        var result = await _sut.DeleteAsync(1);

        result.Success.Should().BeTrue();
        category.IsDeleted.Should().BeTrue();

        _categoryRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.Category(1)), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }

    #endregion
}
