
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepo _categoryRepo;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepo categoryRepo, ILogger<CategoryService> logger)
        {
            _categoryRepo = categoryRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetAllAsync(CategoryFilterDto filter, PaginationDto pagination)
        {
            _logger.LogInformation(
                        "Get categories request Page:{Page} Size:{Size}",
                        pagination.PageNumber,
                        pagination.PageSize);
            //  var (items, totalItems) = await _categoryRepo.GetAllAsync( pagination);
            var (categories,totalItems) = await _categoryRepo.GetFilteredAsync(filter, pagination); 
             
            var data = categories.Select(c=>MapToResponseDto(c)).ToList();

            var pagedData = new PagedResponse<CategoryResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);

            _logger.LogInformation(
                        "Get categories success Count:{Count}",
                        totalItems);
            return ApiResponse<PagedResponse<CategoryResponseDto>>.SuccessResponse(pagedData, "Get data successfully");
        }
    

        public async Task<ApiResponse<CategoryResponseDto?>> GetByIdAsync(int id)
        {
            _logger.LogInformation("Get category by id {CategoryId}", id);
            var category = await _categoryRepo.GetByIdAsync(id);

            if (category == null)
            {
                _logger.LogWarning("Category not found {CategoryId}", id);
                throw new NotFoundException("Category not found");
            }
            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto?>.SuccessResponse(
                     item,
                     "Create data successfully"
                    );
        }

        public async Task<ApiResponse<CategoryResponseDto>> CreateAsync(CategoryCreateDto dto)
        {
            _logger.LogInformation("Create category {Name}", dto.Name);
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                Slug = dto.Slug,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };
            await _categoryRepo.CreateAsync(category);

            _logger.LogInformation("Category created {CategoryId}", category.Id);

            var item = MapToResponseDto(category);
            return  ApiResponse<CategoryResponseDto>.SuccessResponse(
                   item,
                    "Create data successfully"
                    );
        }

        public async Task<ApiResponse<CategoryResponseDto>> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            _logger.LogInformation("Update category {CategoryId}", id);
            var category = await _categoryRepo.GetByIdForUpdateAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Update failed category not found {CategoryId}", id);
                throw new NotFoundException("Category not found");
            }
            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Slug = dto.Slug;
            category.UpdatedAt = DateTime.UtcNow;

            await _categoryRepo.UpdateAsync(category);

            _logger.LogInformation("Category updated {CategoryId}", category.Id);

            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(
                   item,
                   "Update data successfully"
                   );
        }

        public async Task<ApiResponse<CategoryResponseDto>> DeleteAsync(int id)
        {
            _logger.LogInformation("Delete category {CategoryId}", id);
            var category = await _categoryRepo.GetByIdForUpdateAsync(id);

            if (category == null)
            {
                _logger.LogWarning("Delete failed category not found {CategoryId}", id);
                throw new NotFoundException("Category not found");
            }
            category.IsDeleted = true;

            await _categoryRepo.SaveChangesAsync();

            _logger.LogInformation("Category deleted {CategoryId}", category.Id);

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
