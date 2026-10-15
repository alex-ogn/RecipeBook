using System.Collections;

namespace RecipeBook.ViewModels.Shared
{
    /// <summary>
    /// Paging information shown by the <c>_Pager</c> partial.
    /// </summary>
    public interface IPagedResult
    {
        int Page { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
    }

    /// <summary>
    /// One page of items plus the paging information. Enumerates only the items on the current page.
    /// </summary>
    public class PagedResult<T> : IPagedResult, IReadOnlyList<T>
    {
        public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public IReadOnlyList<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

        public int Count => Items.Count;
        public T this[int index] => Items[index];

        /// <summary>
        /// Same page with each item converted, e.g. from a query projection to a view model.
        /// </summary>
        public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector)
            => new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
