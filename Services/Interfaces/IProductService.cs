using Ecommerce_API.DTOs.ProductDtos;
using Ecommerce_API.Helpers;

namespace Ecommerce_API.Services.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<ProductResponseDto>> GetAllAsync(Pagination pagination);
        Task<ProductResponseDto?> GetByIdAsync(int id);
        Task<ProductResponseDto> CreateAsync(ProductCreateDto dto);
        Task UpdateAsync(int id, ProductUpdateDto dto);
        Task DeleteAsync(int id);
    }
}
