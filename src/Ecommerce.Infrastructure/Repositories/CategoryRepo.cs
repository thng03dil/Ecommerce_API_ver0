using Ecommerce.Infrastructure.Data;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Extensions;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Ecommerce.Infrastructure.Repositories
{
    public class CategoryRepo : ICategoryRepo
    {

        private readonly AppDbContext _context;
        public CategoryRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Category>, int)> GetFilteredAsync(
            CategoryFilterDto filter,
            PaginationDto pagination)
        {
            // 1. Chỉ dùng 1 query duy nhất và nạp Products ngay từ đầu
            var query = _context.Categories
                .Include(c => c.Products.Where(p => !p.IsDeleted)) // Nạp sản phẩm chưa xóa
                .Where(x => !x.IsDeleted)
                .ApplySearch(filter.Keyword, c => c.Name)
                .ApplySearch(filter.Slug, c => c.Slug)
                .ApplySorting(filter.SortBy ?? "Id", filter.SortOrder ?? "asc");

            var total = await query.CountAsync();

            // 2. Lấy dữ liệu nguyên bản (không dùng Select thủ công gán lại Products)
            var items = await query
                .ApplyPagination(pagination.PageNumber, pagination.PageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<Category?> GetByIdAsync(int id) { 
            return await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }
        
        public async Task AddAsync(Category category) {
            
               await _context.Categories.AddAsync(category);
        }

        public async Task<Category?> GetByIdForUpdateAsync(int id)
        {
            return await _context.Categories
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<bool> HasActiveProductsAsync(int categoryId)
        {
            return await _context.Products.AnyAsync(p => p.CategoryId == categoryId && !p.IsDeleted);
        }

        public async Task UpdateAsync(Category category) {

            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync() {
            await _context.SaveChangesAsync();
        }
       
    }
}
