
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepo _categoryRepo;

        public CategoryService(ICategoryRepo categoryRepo)
        {
            _categoryRepo = categoryRepo;
        }
        
        public async Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetAllAsync(CategoryFilterDto filter, PaginationDto pagination)
        {
            var (categories,totalItems) = await _categoryRepo.GetFilteredAsync(filter, pagination); 
             
            var data = categories.Select(c=>MapToResponseDto(c)).ToList();

            var pagedData = new PagedResponse<CategoryResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);

            return ApiResponse<PagedResponse<CategoryResponseDto>>.SuccessResponse(pagedData, "Get data successfully");
        }
    

        public async Task<ApiResponse<CategoryResponseDto?>> GetByIdAsync(int id)
        {
            var category = await _categoryRepo.GetByIdAsync(id);

            if (category == null)  throw new NotFoundException("Category not found");
            
            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto?>.SuccessResponse(
                     item,
                     "Get data successfully"
                    );
        }

        public async Task<ApiResponse<CategoryResponseDto>> CreateAsync(CategoryCreateDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                Slug = dto.Slug,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
            await _categoryRepo.CreateAsync(category);

            var item = MapToResponseDto(category);
            return  ApiResponse<CategoryResponseDto>.SuccessResponse(
                   item,
                    "Create data successfully"
                    );
        }

        public async Task<ApiResponse<CategoryResponseDto>> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var category = await _categoryRepo.GetByIdForUpdateAsync(id);
            if (category == null)  throw new NotFoundException("Category not found");
            
            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Slug = dto.Slug;
            category.UpdatedAt = DateTime.UtcNow;

            await _categoryRepo.UpdateAsync(category);

            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(
                   item,
                   "Update data successfully"
                   );
        }

        public async Task<ApiResponse<CategoryResponseDto>> DeleteAsync(int id)
        {
            var category = await _categoryRepo.GetByIdForUpdateAsync(id);

            if (category == null)
            {
                throw new NotFoundException("Category not found");
            }

            // Business rule: cannot delete a category that still has active products.
            if (await _categoryRepo.HasActiveProductsAsync(id))
            {
                throw new BusinessException("Cannot delete this category because it still contains products.");
            }

            category.IsDeleted = true;

            await _categoryRepo.SaveChangesAsync();

            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(
                     item,
                    "Delete data successfully"
            );
        }
        private static CategoryResponseDto MapToResponseDto(Category c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Slug = c.Slug,
            ProductCount = c.Products?.Count() ?? 0,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        };
    }
}
