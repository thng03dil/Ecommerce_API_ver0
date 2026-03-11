using Ecommerce_API.DTOs.ProductDtos;
using Ecommerce_API.Helpers.Pagination;
using Ecommerce_API.DTOs.Common;
using Ecommerce_API.DTOs.CategoryDtos;
using Ecommerce_API.Helpers.Responses;

namespace Ecommerce_API.Services.Interfaces
{
    public interface IProductService
    {
        Task<PagedResponse<ProductResponseDto>> GetAllAsync(PaginationDto pagedto);
        Task<ApiResponse<ProductResponseDto?>> GetByIdAsync(int id);
        Task<ApiResponse<ProductResponseDto>> CreateAsync(ProductCreateDto dto);
        Task<ApiResponse<ProductResponseDto>> UpdateAsync(int id, ProductUpdateDto dto);
        Task<ApiResponse<ProductResponseDto>> DeleteAsync(int id);
    }
}
