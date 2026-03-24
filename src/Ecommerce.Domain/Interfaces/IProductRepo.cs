using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces
{
    public interface IProductRepo
    {
        Task<bool> CategoryExistsAsync(int categoryId);

        /// <summary>Kiểm tra đã có sản phẩm (chưa xóa mềm) cùng tên — so khớp không phân biệt hoa thường.</summary>
        Task<bool> ExistsByNameAsync(string name, int? excludeProductId = null);

        Task LoadCategoryAsync(Product product); 

        Task AddAsync(Product product);

        Task UpdateAsync(Product product);

        Task SaveChangesAsync();

        Task<Product?> GetByIdAsync(int id);

        Task<(IEnumerable<Product>, int)> GetFilteredAsync(ProductFilterDto filter, PaginationDto pagination); 

    }
}
