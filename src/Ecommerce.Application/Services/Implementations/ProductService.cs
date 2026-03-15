
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Domain.Entities;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Application.Common.Pagination;

namespace Ecommerce.Application.Services.Implementations
{
    public class ProductService : IProductService
    {

        private readonly IProductRepo _productRepo;
        public ProductService(
            IProductRepo productRepo)
        {
            _productRepo = productRepo;
        }

        public async Task<ApiResponse<PagedResponse<ProductResponseDto>>> GetAllAsync(ProductFilterDto filter, PaginationDto pagination)
        {
            //  var (products, totalItems) = await _productRepo.GetAllAsync( pagination);
            var (products, totalItems) = await _productRepo.GetFilteredAsync(filter, pagination);

            var data = products.Select(c => MapToResponseDto(c)).ToList();
            var pagedData = new PagedResponse<ProductResponseDto>(data, pagination.PageNumber, pagination.PageSize, totalItems);
            return ApiResponse<PagedResponse<ProductResponseDto>>.SuccessResponse(pagedData, "Get data successfully"); 
        }

        public async Task<ApiResponse<ProductResponseDto?>> GetByIdAsync(int id)
        { 
            var product = await _productRepo.GetByIdAsync(id);

            if (product == null)
                throw new NotFoundException("Product not found");

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto?>.SuccessResponse(
                    item,
                    "Get data successfully"
                );
        }

        public async Task<ApiResponse<ProductResponseDto>> CreateAsync(ProductCreateDto dto)
        {
            
            // Validate Category exist
             var categoryExist = await _productRepo.CategoryExistsAsync(dto.CategoryId);

            if (!categoryExist ) throw new NotFoundException("Category not found");

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
                    throw new NotFoundException("Category not found");

                product.CategoryId = dto.CategoryId;
            }

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepo.UpdateAsync(product);

            var item = MapToResponseDto(product);
            return ApiResponse<ProductResponseDto>.SuccessResponse(
                      item,
                      "Update data successfully"
                     );
        }

        public async Task<ApiResponse<ProductResponseDto>> DeleteAsync(int id)
        {
            var product = await _productRepo.GetByIdAsync(id); 

            if (product == null)
                throw new NotFoundException("Product not found");

            product.IsDeleted = true;
            await _productRepo.SaveChangesAsync();

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