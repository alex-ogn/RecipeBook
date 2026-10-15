using Microsoft.EntityFrameworkCore;
using RecipeBook.ViewModels.Shared;

namespace RecipeBook.Extensions
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Runs a count query and a page query. Pages below 1 become 1; pages past the end become the last page.
        /// The query must already be ordered so pages are stable.
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
        {
            var totalCount = await query.CountAsync();
            var lastPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            page = Math.Clamp(page, 1, lastPage);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>(items, page, pageSize, totalCount);
        }
    }
}
