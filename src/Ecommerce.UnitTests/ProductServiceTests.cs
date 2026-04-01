using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepo> _productRepo = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_productRepo.Object, _cacheService.Object);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ShouldAlwaysLoadFromRepository()
    {
        var filter = TestDataMother.CreateProductFilter();
        var pagination = TestDataMother.CreatePagination();
        var product = TestDataMother.CreateProduct(20, 1, "Listed");

        _productRepo
            .Setup(x => x.GetFilteredAsync(filter, pagination))
            .ReturnsAsync(([product], 1));

        var result = await _sut.GetAllAsync(filter, pagination);

        result.Success.Should().BeTrue();
        result.Data!.Data.Should().ContainSingle(x => x.Id == 20 && x.Name == "Listed");
        _productRepo.Verify(x => x.GetFilteredAsync(filter, pagination), Times.Once);
        _cacheService.Verify(x => x.GetAsync<PagedResponse<ProductResponseDto>>(It.IsAny<string>()), Times.Never);
        _cacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResponse<ProductResponseDto>>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ShouldLoadFromDatabase()
    {
        var product = TestDataMother.CreateProduct(1, 1, "Test Product");
        _productRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(product);

        var result = await _sut.GetByIdAsync(1);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(1);
        result.Data.Name.Should().Be("Test Product");
        result.Data.Price.Should().Be(10m);
        result.Data.CategoryName.Should().Be("Cat");

        _productRepo.Verify(x => x.GetByIdAsync(1), Times.Once);
        _cacheService.Verify(x => x.GetAsync<ProductResponseDto>(It.IsAny<string>()), Times.Never);
        _cacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<ProductResponseDto>(), It.IsAny<TimeSpan?>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _productRepo.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Product?)null);

        var act = () => _sut.GetByIdAsync(999);

        // SỬA: Dùng .Message
        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Message.Should().Be("Product with ID 999 not found");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WhenCategoryMissing_ShouldThrowNotFoundException()
    {
        _productRepo.Setup(x => x.CategoryExistsAsync(It.IsAny<int>())).ReturnsAsync(false);
        var dto = TestDataMother.CreateProductCreateDto(99);

        var act = () => _sut.CreateAsync(dto);

        // SỬA: Dùng .Message
        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Message.Should().Be("Category not found");
    }

    [Fact]
    public async Task CreateAsync_WhenValid_ShouldPersistLoadCategoryAndBumpCategoryVersionOnly()
    {
        _productRepo.Setup(x => x.CategoryExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        _productRepo
            .Setup(x => x.LoadCategoryAsync(It.IsAny<Product>()))
            .Callback<Product>(p => p.Category = new Category { Id = 1, Name = "Electronics" })
            .Returns(Task.CompletedTask);
        var dto = TestDataMother.CreateProductCreateDto(1);

        var result = await _sut.CreateAsync(dto);

        result.Success.Should().BeTrue();
        result.Data!.CategoryName.Should().Be("Electronics");

        _productRepo.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
        _productRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _productRepo.Verify(x => x.LoadCategoryAsync(It.IsAny<Product>()), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_ShouldThrowBusinessException()
    {
        _productRepo.Setup(x => x.CategoryExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(x => x.ExistsByNameAsync("New", It.IsAny<int?>())).ReturnsAsync(true);
        var dto = TestDataMother.CreateProductCreateDto(1);

        var act = () => _sut.CreateAsync(dto);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("A product with this name already exists.");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _productRepo.Setup(x => x.GetByIdAsync(50)).ReturnsAsync((Product?)null);
        var dto = TestDataMother.CreateProductUpdateDto(50);

        var act = () => _sut.UpdateAsync(50, dto);

        // SỬA: Dùng .Message
        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Message.Should().Be("Product not found");
    }

    [Fact]
    public async Task UpdateAsync_WhenTargetCategoryMissing_ShouldThrowNotFoundException()
    {
        var product = TestDataMother.CreateProduct(12, 1);
        _productRepo.Setup(x => x.GetByIdAsync(12)).ReturnsAsync(product);
        _productRepo.Setup(x => x.CategoryExistsAsync(99)).ReturnsAsync(false);
        var dto = TestDataMother.CreateProductUpdateDto(12, 99);

        var act = () => _sut.UpdateAsync(12, dto);

        // SỬA: Dùng .Message
        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Message.Should().Be("Category not found");
    }


    [Fact]
    public async Task UpdateAsync_WhenValid_ShouldUpdateAndBumpCategoryVersionOnly()
    {
        var product = TestDataMother.CreateProduct(12, 1);
        _productRepo.Setup(x => x.GetByIdAsync(12)).ReturnsAsync(product);
        _productRepo.Setup(x => x.CategoryExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), 12)).ReturnsAsync(false);
        var dto = TestDataMother.CreateProductUpdateDto(12, 1);

        var result = await _sut.UpdateAsync(12, dto);

        result.Success.Should().BeTrue();
        _productRepo.Verify(x => x.UpdateAsync(product), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDuplicateName_ShouldThrowBusinessException()
    {
        var product = TestDataMother.CreateProduct(12, 1, "Old");
        _productRepo.Setup(x => x.GetByIdAsync(12)).ReturnsAsync(product);
        _productRepo.Setup(x => x.CategoryExistsAsync(1)).ReturnsAsync(true);
        _productRepo.Setup(x => x.ExistsByNameAsync("Upd", 12)).ReturnsAsync(true);
        var dto = TestDataMother.CreateProductUpdateDto(12, 1);

        var act = () => _sut.UpdateAsync(12, dto);

        (await act.Should().ThrowAsync<BusinessException>()).Which.ErrorCode.Should()
            .Be("A product with this name already exists.");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        _productRepo.Setup(x => x.GetByIdAsync(8)).ReturnsAsync((Product?)null);

        var act = () => _sut.DeleteAsync(8);

        // SỬA: Dùng .Message
        (await act.Should().ThrowAsync<NotFoundException>())
            .Which.Message.Should().Be("Product not found");
    }

    [Fact]
    public async Task DeleteAsync_WhenValid_ShouldSoftDeleteAndBumpCategoryVersionOnly()
    {
        var product = TestDataMother.CreateProduct(8, 2);
        _productRepo.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(product);

        var result = await _sut.DeleteAsync(8);

        result.Success.Should().BeTrue();
        product.IsDeleted.Should().BeTrue();
        _productRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }

    #endregion
}
