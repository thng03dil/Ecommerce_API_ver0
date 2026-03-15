using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Domain.Common.Filters;

namespace Ecommerce.Application.Services.Interfaces

{
    public interface IProductService
    {
        Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetAllAsync(ProductFilterDto filter, PaginationDto pagination);
        Task<ApiResponse<ProductResponseDto?>> GetByIdAsync(int id);
        Task<ApiResponse<ProductResponseDto>> CreateAsync(ProductCreateDto dto);
        Task<ApiResponse<ProductResponseDto>> UpdateAsync(int id, ProductUpdateDto dto);
        Task<ApiResponse<ProductResponseDto>> DeleteAsync(int id);
    }
}
  