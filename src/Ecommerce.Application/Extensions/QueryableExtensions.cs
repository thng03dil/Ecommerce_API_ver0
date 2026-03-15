using System.Linq.Expressions;
using System.Reflection;

namespace Ecommerce.Application.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplySorting<T>(
    this IQueryable<T> query,
    string? sortBy,
    string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query;

            var property = typeof(T).GetProperty(
                sortBy,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
             
            if (property == null)
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");

            var propertyAccess = Expression.Property(parameter, property);

            var orderByExp = Expression.Lambda(propertyAccess, parameter); 

            var method = sortOrder?.ToLower() == "desc"
                ? "OrderByDescending"
                : "OrderBy";

            var resultExp = Expression.Call(
                typeof(Queryable),
                method,
                new[] { typeof(T), property.PropertyType },
                query.Expression,
                Expression.Quote(orderByExp));

            return query.Provider.CreateQuery<T>(resultExp);
        }

        public static IQueryable<T> ApplySearch<T>(
                this IQueryable<T> query,
                string? keyword,
                Expression<Func<T, string>> property)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return query;

            var parameter = property.Parameters[0];

            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            var body = Expression.Call(property.Body, containsMethod!, Expression.Constant(keyword));

            var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);

            return query.Where(predicate);
        }
        public static IQueryable<T> ApplyPagination<T>(
                this IQueryable<T> query,
                int pageNumber,
                int pageSize)
        {
            return query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }
    }
}
