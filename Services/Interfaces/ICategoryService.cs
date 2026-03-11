using Ecommerce_API.DTOs.CategoryDtos;
using Ecommerce_API.DTOs.Common;
using Ecommerce_API.Helpers.Pagination;
using Ecommerce_API.Helpers.Responses;
namespace Ecommerce_API.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<PagedResponse<CategoryResponseDto>> GetAllAsync(PaginationDto pagedto);
        Task<ApiResponse<CategoryResponseDto?>> GetByIdAsync(int id);
        Task<ApiResponse<CategoryResponseDto>> CreateAsync(CategoryCreateDto dto);
        Task<ApiResponse<CategoryResponseDto>> UpdateAsync(int id, CategoryUpdateDto dto);
       // Task<CategoryResponseDto> PatchAsync(int id, CategoryUpdateDto dto);
        Task<ApiResponse<CategoryResponseDto>> DeleteAsync(int id);
    }
}
