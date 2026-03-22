using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces
{
    public interface IProductRepo
    {
        Task<bool> CategoryExistsAsync(int categoryId);

        Task LoadCategoryAsync(Product product); 

        Task CreateAsync(Product product);

        Task UpdateAsync(Product product);

        Task SaveChangesAsync();

        Task<Product?> GetByIdAsync(int id);

        Task<(IEnumerable<Product>, int)> GetFilteredAsync(ProductFilterDto filter, PaginationDto pagination); 

    }
}
