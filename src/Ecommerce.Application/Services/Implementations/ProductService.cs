using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IProductRepo _productRepo;
        private readonly ICacheService _cacheService;

        public ProductService(IProductRepo productRepo, ICacheService cacheService)
        {
            _productRepo = productRepo;
            _cacheService = cacheService;
        }

        public async Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetAllAsync(ProductFilterDto filter, PaginationDto pagination)
        {
            var (products, totalItems) = await _productRepo.GetFilteredAsync(filter, pagination);
            var data = products.Select(MapToResponseDto).ToList();
            var pagedData = new PagedResponse<ProductResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);
            return ApiResponse<PagedResponse<ProductResponseDto>>.SuccessResponse(pagedData, "Get data successfully");
        }

        public async Task<ApiResponse<ProductResponseDto?>> GetByIdAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);

            if (product == null)
                throw new NotFoundException($"Product with ID {id} not found");

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto?>.SuccessResponse(item, "Get data successfully");
        }

        public async Task<ApiResponse<ProductResponseDto>> CreateAsync(ProductCreateDto dto)
        {
            var categoryExist = await _productRepo.CategoryExistsAsync(dto.CategoryId);

            if (!categoryExist) throw new NotFoundException("Category not found");

            var name = dto.Name?.Trim() ?? string.Empty;
            if (await _productRepo.ExistsByNameAsync(name))
                throw new BusinessException("A product with this name already exists.");

            var product = new Product
            {
                CategoryId = dto.CategoryId,
                Name = name,
                Price = dto.Price,
                Description = dto.Description?.Trim() ?? string.Empty,
                Stock = dto.Stock,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            await _productRepo.AddAsync(product);
            await _productRepo.SaveChangesAsync();

            await _productRepo.LoadCategoryAsync(product);

            await _cacheService.RemoveAsync(CacheKeyGenerator.Category(product.CategoryId));

            await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto>.SuccessResponse(
                item,
                "Create data successfully");
        }

        public async Task<ApiResponse<ProductResponseDto>> UpdateAsync(int id, ProductUpdateDto dto)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
                throw new NotFoundException("Product not found");

            if (dto.CategoryId != 0 && dto.CategoryId != product.CategoryId)
            {
                var categoryExists = await _productRepo.CategoryExistsAsync(dto.CategoryId);

                if (!categoryExists)
                    throw new NotFoundException("Category not found");
                product.CategoryId = dto.CategoryId;
            }

            var newName = dto.Name?.Trim() ?? string.Empty;
            if (await _productRepo.ExistsByNameAsync(newName, id))
                throw new BusinessException("A product with this name already exists.");

            product.Name = newName;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepo.UpdateAsync(product);

            await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

            var updatedProduct = await _productRepo.GetByIdAsync(id);
            var item = MapToResponseDto(updatedProduct!);
            return ApiResponse<ProductResponseDto>.SuccessResponse(
                item,
                "Update data successfully");
        }

        public async Task<ApiResponse<ProductResponseDto>> DeleteAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);

            if (product == null) throw new NotFoundException("Product not found");

            product.IsDeleted = true;
            await _productRepo.SaveChangesAsync();

            await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

            var item = MapToResponseDto(product);

            return ApiResponse<ProductResponseDto>.SuccessResponse(
                item,
                "Delete data successfully");
        }

        private static ProductResponseDto MapToResponseDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? "Unknown"
        };
    }
}
