using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests
{
    public class ProductServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_WhenProductExists_ReturnsSuccess()
        {
            var product = new Product
            {
                Id = 1,
                Name = "Test Product",
                Price = 99.99m,
                Stock = 10,
                CategoryId = 1,
                Category = new Category { Id = 1, Name = "Electronics" }
            };

            var mockProductRepo = new Mock<IProductRepo>();
            mockProductRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(product);

            var mockCache = new Mock<ICacheService>();
            mockCache.Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<ProductResponseDto?>>>(), It.IsAny<TimeSpan?>()))
                .Returns<string, Func<Task<ProductResponseDto?>>, TimeSpan?>((_, factory, _) => factory());

            var service = new ProductService(mockProductRepo.Object, mockCache.Object);
            var result = await service.GetByIdAsync(1);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal(1, result.Data!.Id);
            Assert.Equal("Test Product", result.Data.Name);
            Assert.Equal(99.99m, result.Data.Price);
        }

        [Fact]
        public async Task GetByIdAsync_WhenProductNotFound_ThrowsNotFoundException()
        {
            var mockProductRepo = new Mock<IProductRepo>();
            mockProductRepo.Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((Product?)null);

            var mockCache = new Mock<ICacheService>();
            mockCache.Setup(x => x.GetOrSetAsync(It.IsAny<string>(), It.IsAny<Func<Task<ProductResponseDto?>>>(), It.IsAny<TimeSpan?>()))
                .Returns<string, Func<Task<ProductResponseDto?>>, TimeSpan?>((_, factory, _) => factory());

            var service = new ProductService(mockProductRepo.Object, mockCache.Object);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(999));
            Assert.Equal("Product not found", ex.ErrorCode);
        }
    }
}
