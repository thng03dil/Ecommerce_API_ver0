using Ecommerce.Infrastructure.Data;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Application.Extensions;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Ecommerce.Infrastructure.Repositories
{
    public class ProductRepo : IProductRepo
    {
        private readonly AppDbContext _context;
        public ProductRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Product>, int)> GetFilteredAsync(
            ProductFilterDto filter,
            PaginationDto pagination)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(filter.Keyword))
            {
                query = query.Where(p =>
                    p.Name.Contains(filter.Keyword));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId == filter.CategoryId);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p =>
                    p.Price >= filter.MinPrice);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p =>
                    p.Price <= filter.MaxPrice);
            }

            query = query.ApplySorting(
                filter.SortBy ?? "Id",
                filter.SortOrder ?? "asc");

            var total = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<bool> CategoryExistsAsync(int categoryId)
        {
            return await _context.Categories
                .AnyAsync(c => c.Id == categoryId && !c.IsDeleted);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var trimmed = name.Trim();
            return await _context.Products
                .AnyAsync(p =>
                    p.Name.ToLower() == trimmed.ToLower() &&
                    (!excludeProductId.HasValue || p.Id != excludeProductId.Value));
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }
        //Explicit Loading (Tải tường minh) nạp bù dữ liệu
        public async Task LoadCategoryAsync(Product product)
        {
            await _context.Entry(product)
                .Reference(p => p.Category)
                .LoadAsync();
        }


        public async Task<Product?> GetByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }
       
        public async Task UpdateAsync(Product product)
        {

            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            await _context.Entry(product).Reference(p => p.Category).LoadAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

       
    }
}
