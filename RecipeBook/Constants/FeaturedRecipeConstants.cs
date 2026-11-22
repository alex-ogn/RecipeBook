namespace RecipeBook.Constants;

/// <summary>
/// The home page banner: recipes an administrator features, shown as a background slideshow.
/// </summary>
public static class FeaturedRecipeConstants
{
    /// <summary>
    /// While no recipe is featured, the banner shows this many of the most liked recipes that have a photo.
    /// </summary>
    public const int DefaultCount = 4;

    /// <summary>How long each photo stays before the next one fades in.</summary>
    public const int SlideIntervalMilliseconds = 6000;
}
