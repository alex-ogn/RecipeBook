namespace RecipeBook.Services
{
    public enum DeleteStatus
    {
        Deleted,
        NotFound,
        InUse
    }

    /// <summary>
    /// Outcome of deleting an entity that other records may still reference.
    /// </summary>
    /// <param name="Status">What happened.</param>
    /// <param name="UsedBy">Names of the records that still use the entity when <see cref="DeleteStatus.InUse"/>.</param>
    public record DeleteResult(DeleteStatus Status, IReadOnlyList<string> UsedBy)
    {
        public static DeleteResult Deleted() => new(DeleteStatus.Deleted, Array.Empty<string>());
        public static DeleteResult NotFound() => new(DeleteStatus.NotFound, Array.Empty<string>());
        public static DeleteResult InUse(IReadOnlyList<string> usedBy) => new(DeleteStatus.InUse, usedBy);
    }
}
