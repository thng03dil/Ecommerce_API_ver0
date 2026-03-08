using Ecommerce_API.Data;
using Ecommerce_API.DTOs.CategoryDtos;
using Ecommerce_API.Helpers;
using Ecommerce_API.Models;
using Ecommerce_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_API.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(Pagination pagination)
        {
            var query = _context.Categories
                .Where(x => !x.IsDeleted)
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize);

            return await query
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Slug = c.Slug,
                    ProductCount = c.Products.Count()
                })
                .ToListAsync();
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (category == null) 
                throw new Exception("Category not found");

            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.Slug,
                ProductCount = category.Products?.Count ?? 0
            };
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
        {
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                Slug = dto.Slug
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Slug = category.Slug,                                                        
                ProductCount = 0
            };
        }

        public async Task UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (category == null)
                throw new Exception("Category not found");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Slug = dto.Slug;
            category.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null)
                throw new Exception("Category not found");

            category.IsDeleted = true;

            await _context.SaveChangesAsync();
        }
    }
}
