
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Application.Common.Pagination;

namespace Ecommerce.Domain.Interfaces
{
    public interface ICategoryRepo
    {
        
            Task<Category?> GetByIdAsync(int id);

            Task CreateAsync(Category category);

            Task UpdateAsync(Category category); 

            Task SaveChangesAsync();

            Task<Category?> GetByIdForUpdateAsync(int id);
            Task<bool> HasActiveProductsAsync(int categoryId);
            Task<bool> SlugExistsAsync(string slug, int excludeId);
            Task<(IEnumerable<Category>, int)> GetFilteredAsync(CategoryFilterDto filter, PaginationDto pagination);


    }
}
 