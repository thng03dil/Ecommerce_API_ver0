using Ecommerce_API.Data;
using Ecommerce_API.DTOs.ProductDtos;
using Ecommerce_API.DTOs.Common;
using Ecommerce_API.Helpers.Pagination;
using Ecommerce_API.Models;
using Ecommerce_API.Services.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_API.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly IValidator<ProductCreateDto> _createValidator;
        private readonly IValidator<ProductUpdateDto> _updateValidator;
        public ProductService(
            AppDbContext context,
            IValidator<ProductCreateDto> createValidator,
            IValidator<ProductUpdateDto> updateValidator)
        {
            _context = context;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<PagedResponse<ProductResponseDto>> GetAllAsync(PaginationDto pagedto)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(x => !x.IsDeleted);
            
            var totalItems = await query.CountAsync();

            var items = await query
                .Include(p => p.Category)
                .OrderBy(p => p.Id)
                .Skip((pagedto.PageNumber - 1) * pagedto.PageSize)
                .Take(pagedto.PageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category!.Name,
                    CreateAt = p.CreatedAt,
                    UpdateAt = p.UpdatedAt,

                })
                .ToListAsync();
            return new PagedResponse<ProductResponseDto>(items, pagedto.PageNumber, pagedto.PageSize, totalItems);
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
                throw new Exception("Product not found");

            return MapToResponseDto(product);
        }

        public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
        {
            var result = await _createValidator.ValidateAsync(dto);
            if (!result.IsValid) throw new ValidationException(result.Errors);

            // Validate Category exist
           // var category = await _context.Categories
        //.FirstOrDefaultAsync(c => c.Id == dto.CategoryId);

           // if (category == null) throw new Exception("Category not found");

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

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await _context.Entry(product).Reference(p => p.Category).LoadAsync();

            return MapToResponseDto(product);
        }

        public async Task<ProductResponseDto> UpdateAsync(int id, ProductUpdateDto dto)
        {
            dto.Id = id;

            var result = await _updateValidator.ValidateAsync(dto);
            if (!result.IsValid) throw new ValidationException(result.Errors);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null)
                throw new Exception("Product not found");

            if (dto.CategoryId.HasValue && dto.CategoryId.Value != 0)
            {
                var categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == dto.CategoryId);
                if (!categoryExists)
                    throw new Exception("Category not found");
                product.CategoryId = dto.CategoryId.Value;
            }

            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            product.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToResponseDto(product);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products
        .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
                throw new Exception("Product not found");

            product.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
        private static ProductResponseDto MapToResponseDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            CategoryId = p.CategoryId,
            CategoryName = p.Category!.Name,
        };
    }
}