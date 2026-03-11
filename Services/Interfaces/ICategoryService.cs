using Ecommerce_API.DTOs.CategoryDtos;
using Ecommerce_API.DTOs.Common;
using Ecommerce_API.Helpers.Pagination;
namespace Ecommerce_API.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedResponse<CategoryResponseDto>> GetAllAsync(PaginationDto pagedto);
        Task<CategoryResponseDto?> GetByIdAsync(int id);
        Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto);
        Task<CategoryResponseDto> UpdateAsync(int id, CategoryUpdateDto dto);
        Task DeleteAsync(int id);
    }
}
