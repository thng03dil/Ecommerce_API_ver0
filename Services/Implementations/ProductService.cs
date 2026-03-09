using Ecommerce_API.Data;
using Ecommerce_API.DTOs.ProductDtos;
using Ecommerce_API.Helpers;
using Ecommerce_API.Models;
using Ecommerce_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_API.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllAsync(Pagination pagination)
        {
            var query = _context.Products
                .AsNoTracking()
                .Where(x => !x.IsDeleted);
                

            return await query
                .Include(p => p.Category)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .Select(p => new ProductResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category!.Name
                    
                })
                .ToListAsync();
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
                throw new Exception("Product not found");

            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                CategoryId = product.CategoryId,
                CategoryName = product.Category!.Name,
            };
        }

        public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
        {
            // Validate Category exist
            var categoryExists = await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                throw new Exception("Category not found");

            var product = new Product
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                Stock = dto.Stock
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var category = await _context.Categories.FindAsync(dto.CategoryId);

            return new ProductResponseDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                CategoryName = category!.Name,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                Stock = product.Stock
            };
        }

        public async Task UpdateAsync(int id, ProductUpdateDto dto)
        {
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
            product.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
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
    }
}
