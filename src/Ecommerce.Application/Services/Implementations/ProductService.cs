using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using System.Threading;

namespace Ecommerce.Application.Services.Implementations
{
    public class ProductService : IProductService
    {
        private static readonly TimeSpan ProductCacheTtl = TimeSpan.FromMinutes(10);
        private static readonly SemaphoreSlim _listLoadLock = new(1, 1);

        private readonly IProductRepo _productRepo;
        private readonly ICacheService _cacheService;

        public ProductService(IProductRepo productRepo, ICacheService cacheService)
        {
            _productRepo = productRepo;
            _cacheService = cacheService;
        }

        public async Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetAllAsync(ProductFilterDto filter, PaginationDto pagination)
        {
            var filterHash = CacheKeyGenerator.HashFilter(filter);
            var version = await _cacheService.GetVersionAsync(CacheKeyGenerator.ProductVersionKey());
            var cacheKey = CacheKeyGenerator.ProductList(version, pagination.PageNumber, pagination.PageSize, filterHash);

            var pagedData = await _cacheService.GetAsync<PagedResponse<ProductResponseDto>>(cacheKey);
            if (pagedData != null)
                return ApiResponse<PagedResponse<ProductResponseDto>>.SuccessResponse(pagedData, "Get data successfully");

            await _listLoadLock.WaitAsync();
            try
            {
                version = await _cacheService.GetVersionAsync(CacheKeyGenerator.ProductVersionKey());
                cacheKey = CacheKeyGenerator.ProductList(version, pagination.PageNumber, pagination.PageSize, filterHash);

                pagedData = await _cacheService.GetAsync<PagedResponse<ProductResponseDto>>(cacheKey);
                if (pagedData != null)
                    return ApiResponse<PagedResponse<ProductResponseDto>>.SuccessResponse(pagedData, "Get data successfully");

                var (products, totalItems) = await _productRepo.GetFilteredAsync(filter, pagination);
                var data = products.Select(MapToResponseDto).ToList();
                pagedData = new PagedResponse<ProductResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);
                await _cacheService.SetAsync(cacheKey, pagedData, ProductCacheTtl);
            }
            finally
            {
                _listLoadLock.Release();
            }

            return ApiResponse<PagedResponse<ProductResponseDto>>.SuccessResponse(pagedData!, "Get data successfully");
        }

        public async Task<ApiResponse<ProductResponseDto?>> GetByIdAsync(int id)
        {
            var cacheKey = CacheKeyGenerator.Product(id);

            var item = await _cacheService.GetOrSetAsync(cacheKey, async () =>
            {
                var product = await _productRepo.GetByIdAsync(id);
                if (product == null) return null;
                return MapToResponseDto(product);
            }, ProductCacheTtl);

            if (item == null)
                throw new NotFoundException("Product not found");

            return ApiResponse<ProductResponseDto?>.SuccessResponse(item, "Get data successfully");
        }

        public async Task<ApiResponse<ProductResponseDto>> CreateAsync(ProductCreateDto dto)
        {
            var categoryExist = await _productRepo.CategoryExistsAsync(dto.CategoryId);

            if (!categoryExist) throw new NotFoundException("Category not found");

            var product = new Product
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                Stock = dto.Stock,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _productRepo.CreateAsync(product);

            await _productRepo.LoadCategoryAsync(product);

            await _cacheService.IncrementAsync(CacheKeyGenerator.ProductVersionKey());

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto>.SuccessResponse(
                     item,
                     "Create data successfully"
                    );
        }

        public async Task<ApiResponse<ProductResponseDto>> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Product not found");

            if (dto.CategoryId != 0)
            {
                var categoryExists = await _productRepo.CategoryExistsAsync(dto.CategoryId);

                if (!categoryExists)
                {
                    throw new NotFoundException("Category not found");
                }
                product.CategoryId = dto.CategoryId;
            }

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepo.UpdateAsync(product);

            await _cacheService.RemoveAsync(CacheKeyGenerator.Product(id));
            await _cacheService.IncrementAsync(CacheKeyGenerator.ProductVersionKey());

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto>.SuccessResponse(
                      item,
                      "Update data successfully"
                     );
        }

        public async Task<ApiResponse<ProductResponseDto>> DeleteAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);

            if (product == null) throw new NotFoundException("Product not found");

            product.IsDeleted = true;
            await _productRepo.SaveChangesAsync();

            await _cacheService.RemoveAsync(CacheKeyGenerator.Product(id));
            await _cacheService.IncrementAsync(CacheKeyGenerator.ProductVersionKey());

            var item = MapToResponseDto(product);

            return ApiResponse<ProductResponseDto>.SuccessResponse(
                    item,
                   "Delete data successfully"
                   );
        }
        private static ProductResponseDto MapToResponseDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            CategoryId = p.CategoryId,
            CategoryName = p.Category!.Name
        };
    }
}
