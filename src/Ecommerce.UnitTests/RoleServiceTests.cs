using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests
{
    public class RoleServiceTests
    {
        [Fact]
        public async Task DeleteAsync_WhenRoleIsAdmin_ThrowsBusinessException()
        {
            var adminRole = new Role
            {
                Id = 1,
                Name = "Admin",
                Description = "System admin",
                IsSystem = true
            };

            var mockRoleRepo = new Mock<IRoleRepo>();
            mockRoleRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(adminRole);

            var mockPermissionRepo = new Mock<IPermissionRepo>();
            var mockUserRepo = new Mock<IUserRepo>();
            var mockCache = new Mock<ICacheService>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var service = new RoleService(
                mockRoleRepo.Object,
                mockPermissionRepo.Object,
                mockUserRepo.Object,
                mockCache.Object,
                mockUnitOfWork.Object);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(1));
            Assert.Equal("Cannot delete system role", ex.ErrorCode);

            mockUnitOfWork.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task<IReadOnlyList<int>>>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenRoleNotFound_ThrowsNotFoundException()
        {
            var mockRoleRepo = new Mock<IRoleRepo>();
            mockRoleRepo.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Role?)null);

            var mockPermissionRepo = new Mock<IPermissionRepo>();
            var mockUserRepo = new Mock<IUserRepo>();
            var mockCache = new Mock<ICacheService>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var service = new RoleService(
                mockRoleRepo.Object,
                mockPermissionRepo.Object,
                mockUserRepo.Object,
                mockCache.Object,
                mockUnitOfWork.Object);

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999));
            Assert.Equal("Role not found", ex.ErrorCode);
        }
    }
}
