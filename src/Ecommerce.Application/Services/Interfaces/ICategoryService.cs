using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Application.Common.Responses;

namespace Ecommerce.Application.Services.Interfaces

{
    public interface ICategoryService
    {
        Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetAllAsync(CategoryFilterDto filter, PaginationDto pagination);
        Task<ApiResponse<CategoryResponseDto?>> GetByIdAsync(int id);
        Task<ApiResponse<CategoryResponseDto>> CreateAsync(CategoryCreateDto dto);
        Task<ApiResponse<CategoryResponseDto>> UpdateAsync(int id, CategoryUpdateDto dto);
        Task<ApiResponse<CategoryResponseDto>> DeleteAsync(int id);
    }
}

  