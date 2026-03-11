using Ecommerce_API.DTOs.ProductDtos;
using Ecommerce_API.Helpers.Pagination;
using Ecommerce_API.DTOs.Common;

namespace Ecommerce_API.Services.Interfaces
{
    public interface IProductService
    {
        Task<PagedResponse<ProductResponseDto>> GetAllAsync(PaginationDto pagedto);
        Task<ProductResponseDto?> GetByIdAsync(int id);
        Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);
        Task<ProductResponseDto> UpdateAsync(int id, ProductUpdateDto dto);
        Task DeleteAsync(int id);
    }
}
