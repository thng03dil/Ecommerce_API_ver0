
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Services.Implementations
{
    public class ProductService : IProductService
    {

        private readonly IProductRepo _productRepo;
        private readonly ILogger<ProductService> _logger;
        public ProductService(
            IProductRepo productRepo,
            ILogger<ProductService> logger)
        {
            _productRepo = productRepo;
            _logger = logger;
        }

        public async Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetAllAsync(ProductFilterDto filter, PaginationDto pagination)
        {
            _logger.LogInformation(
                       "Get products request Page:{Page} Size:{Size}",
                       pagination.PageNumber,
                       pagination.PageSize);
            //  var (products, totalItems) = await _productRepo.GetAllAsync( pagination);
            var (products, totalItems) = await _productRepo.GetFilteredAsync(filter, pagination);

            var data = products.Select(c => MapToResponseDto(c)).ToList();
            var pagedData = new PagedResponse<ProductResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);

            _logger.LogInformation(
                       "Get products success Count:{Count}",
                       totalItems);
            return ApiResponse<PagedResponse<ProductResponseDto>>.SuccessResponse(pagedData, "Get data successfully"); 
        }

        public async Task<ApiResponse<ProductResponseDto?>> GetByIdAsync(int id)
        {
            _logger.LogInformation("Get product by id {ProductId}", id);
            var product = await _productRepo.GetByIdAsync(id);

            if (product == null)
            {
                _logger.LogWarning("Product not found {ProductId}", id);
                throw new NotFoundException("Product not found");
            }

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto?>.SuccessResponse(
                    item,
                    "Get data successfully"
                );
        }

        public async Task<ApiResponse<ProductResponseDto>> CreateAsync(ProductCreateDto dto)
        {
            _logger.LogInformation("Create product {Name}", dto.Name);
            // Validate Category exist
            var categoryExist = await _productRepo.CategoryExistsAsync(dto.CategoryId);

            if (!categoryExist ) {
                _logger.LogWarning("Create failed category not found {CategoryId}", dto.CategoryId);
                throw new NotFoundException("Category not found");
            }
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

            _logger.LogInformation("Product created {ProductId}", product.Id);

            await _productRepo.LoadCategoryAsync(product);

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto>.SuccessResponse(
                     item,
                     "Create data successfully"
                    );
        }

        public async Task<ApiResponse<ProductResponseDto>> UpdateAsync(int id, ProductUpdateDto dto)
        {
            _logger.LogInformation("Update product {ProductId}", id);
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null)
            {
                _logger.LogWarning("Update failed:product not found {ProductId}", id);
                throw new NotFoundException("Product not found");
            }

            if (dto.CategoryId != 0)
            {
                _logger.LogInformation("Validating existence of CategoryId: {CategoryId}", dto.CategoryId);
                var categoryExists = await _productRepo.CategoryExistsAsync(dto.CategoryId);

                if (!categoryExists)
                {
                    _logger.LogWarning("Validation failed: CategoryId {CategoryId} does not exist", dto.CategoryId);
                    throw new NotFoundException("Category not found");
                }
                product.CategoryId = dto.CategoryId;
                _logger.LogDebug("Successfully assigned CategoryId: {CategoryId} to product entity", dto.CategoryId);
            }

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepo.UpdateAsync(product);

            _logger.LogInformation("Product updated {CategoryId}", product.Id);

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto>.SuccessResponse(
                      item,
                      "Update data successfully"
                     );
        }

        public async Task<ApiResponse<ProductResponseDto>> DeleteAsync(int id)
        {
            _logger.LogInformation("Delete product {ProductId}", id);
            var product = await _productRepo.GetByIdAsync(id); 

            if (product == null)
            {
                _logger.LogWarning("Delete failed: product not found {ProductId}", id);
                throw new NotFoundException("Product not found");
            }

            product.IsDeleted = true;
            await _productRepo.SaveChangesAsync();

            _logger.LogInformation("Product deleted {ProductId}", product.Id);

            var item = MapToResponseDto(product);

            return  ApiResponse<ProductResponseDto>.SuccessResponse(
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