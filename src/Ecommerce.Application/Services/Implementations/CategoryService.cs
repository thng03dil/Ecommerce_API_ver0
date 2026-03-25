using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.CategoryDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using System.Threading;

namespace Ecommerce.Application.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private static readonly TimeSpan CategoryCacheTtl = TimeSpan.FromHours(1);
        private static readonly SemaphoreSlim _listLoadLock = new(1, 1);
        private static readonly SemaphoreSlim _itemLoadLock = new(1, 1);

        private readonly ICategoryRepo _categoryRepo;
        private readonly ICacheService _cacheService;

        public CategoryService(ICategoryRepo categoryRepo, ICacheService cacheService)
        {
            _categoryRepo = categoryRepo;
            _cacheService = cacheService;
        }

        public async Task<ApiResponse<PagedResponse<CategoryResponseDto>>> GetAllAsync(CategoryFilterDto filter, PaginationDto pagination)
        {
            var filterHash = CacheKeyGenerator.HashFilter(filter);
            var version = await _cacheService.GetVersionAsync(CacheKeyGenerator.CategoryVersionKey());
            var cacheKey = CacheKeyGenerator.CategoryList(version, pagination.PageNumber, pagination.PageSize, filterHash);

            var pagedData = await _cacheService.GetAsync<PagedResponse<CategoryResponseDto>>(cacheKey);
            if (pagedData != null)
                return ApiResponse<PagedResponse<CategoryResponseDto>>.SuccessResponse(pagedData, "Get data successfully");

            await _listLoadLock.WaitAsync();
            try
            {
                version = await _cacheService.GetVersionAsync(CacheKeyGenerator.CategoryVersionKey());
                cacheKey = CacheKeyGenerator.CategoryList(version, pagination.PageNumber, pagination.PageSize, filterHash);

                pagedData = await _cacheService.GetAsync<PagedResponse<CategoryResponseDto>>(cacheKey);
                if (pagedData != null)
                    return ApiResponse<PagedResponse<CategoryResponseDto>>.SuccessResponse(pagedData, "Get data successfully");

                var (categories, totalItems) = await _categoryRepo.GetFilteredAsync(filter, pagination);
                var data = categories.Select(MapToResponseDto).ToList();
                pagedData = new PagedResponse<CategoryResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);
                await _cacheService.SetAsync(cacheKey, pagedData, CategoryCacheTtl);
            }
            finally
            {
                _listLoadLock.Release();
            }

            return ApiResponse<PagedResponse<CategoryResponseDto>>.SuccessResponse(pagedData!, "Get data successfully");
        }

        public async Task<ApiResponse<CategoryResponseDto?>> GetByIdAsync(int id)
        {
            var cacheKey = CacheKeyGenerator.Category(id);
            var item = await _cacheService.GetAsync<CategoryResponseDto>(cacheKey);
            if (item != null)
                return ApiResponse<CategoryResponseDto?>.SuccessResponse(item, "Get data successfully");

            await _itemLoadLock.WaitAsync();
            try
            {
                //  Double-check sau khi vào Lock
                item = await _cacheService.GetAsync<CategoryResponseDto>(cacheKey);
                if (item != null) return ApiResponse<CategoryResponseDto?>.SuccessResponse(item, "Get data successfully");

                var category = await _categoryRepo.GetByIdAsync(id);
                if (category == null) throw new NotFoundException("Category not found");

                item = MapToResponseDto(category);
                await _cacheService.SetAsync(cacheKey, item, CategoryCacheTtl);
            }
            finally {
                _itemLoadLock.Release();
            }

            return ApiResponse<CategoryResponseDto?>.SuccessResponse(item, "Get data successfully");
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
            await _categoryRepo.AddAsync(category);
            await _categoryRepo.SaveChangesAsync();

            await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(
                item,
                "Create data successfully");
        }

        public async Task<ApiResponse<CategoryResponseDto>> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var category = await _categoryRepo.GetByIdForUpdateAsync(id);
            if (category == null) throw new NotFoundException("Category not found");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Slug = dto.Slug;
            category.UpdatedAt = DateTime.UtcNow;

            await _categoryRepo.UpdateAsync(category);

            await _cacheService.RemoveAsync(CacheKeyGenerator.Category(id));
            await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());
            await _cacheService.IncrementAsync(CacheKeyGenerator.ProductVersionKey());

            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(
                item,
                "Update data successfully");
        }

        public async Task<ApiResponse<CategoryResponseDto>> DeleteAsync(int id)
        {
            var category = await _categoryRepo.GetByIdForUpdateAsync(id);

            if (category == null)
                throw new NotFoundException("Category not found");

            if (await _categoryRepo.HasActiveProductsAsync(id))
                throw new BadRequestException("Cannot delete category with linked products");

            category.IsDeleted = true;

            await _categoryRepo.SaveChangesAsync();

            await _cacheService.RemoveAsync(CacheKeyGenerator.Category(id));
            await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());
            await _cacheService.IncrementAsync(CacheKeyGenerator.ProductVersionKey());

            var item = MapToResponseDto(category);
            return ApiResponse<CategoryResponseDto>.SuccessResponse(
                item,
                "Delete data successfully");
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
