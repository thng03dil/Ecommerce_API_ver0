using Ecommerce_API.DTOs.CategoryDtos;
using Ecommerce_API.Helpers;
namespace Ecommerce_API.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync(Pagination pagination);
        Task<CategoryResponseDto?> GetByIdAsync(int id);
        Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto);
        Task UpdateAsync(int id, CategoryUpdateDto dto);
        Task DeleteAsync(int id);
    }
}
