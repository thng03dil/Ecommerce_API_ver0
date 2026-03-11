using Ecommerce_API.Data;
using Ecommerce_API.DTOs.CategoryDtos;
using Ecommerce_API.DTOs.Common;
using Ecommerce_API.Models;
using Ecommerce_API.Services.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Ecommerce_API.Helpers.Pagination;

namespace Ecommerce_API.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;
        private readonly IValidator<CategoryCreateDto> _createValidator;
        private readonly IValidator<CategoryUpdateDto> _updateValidator;

        public CategoryService(
            AppDbContext context,
            IValidator<CategoryCreateDto> createValidator,
            IValidator<CategoryUpdateDto> updateValidator)
        {
            _context = context;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<PagedResponse<CategoryResponseDto>> GetAllAsync(PaginationDto pagedto)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.Id)
                .Skip((pagedto.PageNumber - 1) * pagedto.PageSize)
                .Take(pagedto.PageSize)
                .Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    Slug = c.Slug,
                    ProductCount = c.Products.Count(),
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
            return new PagedResponse<CategoryResponseDto>(items, pagedto.PageNumber, pagedto.PageSize, totalItems);
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Include(c => c.Products)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (category == null)
                throw new Exception("Category not found");
           
            return MapToResponseDto(category);
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
        {
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
            var category = new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                Slug = dto.Slug,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            return MapToResponseDto(category);
        }

        public async Task<CategoryResponseDto> UpdateAsync(int id, CategoryUpdateDto dto)
        {
            dto.Id = id;

            var validationResult = await _updateValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (category == null)
                throw new Exception("Category not found");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Slug = dto.Slug;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToResponseDto(category);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories
        .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (category == null)
                throw new KeyNotFoundException("Category not found");

            category.IsDeleted = true;

            await _context.SaveChangesAsync();
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
