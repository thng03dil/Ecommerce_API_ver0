using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests
{
    public class CategoryServiceTests
    {
        [Fact]
        public async Task DeleteAsync_WhenCategoryHasActiveProducts_ThrowsBadRequestException()
        {
            var category = new Category
            {
                Id = 1,
                Name = "Electronics",
                Slug = "electronics"
            };

            var mockCategoryRepo = new Mock<ICategoryRepo>();
            mockCategoryRepo.Setup(x => x.GetByIdForUpdateAsync(1)).ReturnsAsync(category);
            mockCategoryRepo.Setup(x => x.HasActiveProductsAsync(1)).ReturnsAsync(true);

            var mockCache = new Mock<ICacheService>();

            var service = new CategoryService(mockCategoryRepo.Object, mockCache.Object);

            var ex = await Assert.ThrowsAsync<BadRequestException>(() => service.DeleteAsync(1));
            Assert.Equal("Cannot delete category with linked products", ex.Message);

            mockCategoryRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
        }
    }
}
