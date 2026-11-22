namespace RecipeBook.Services;

/// <summary>
/// Outcome of putting a recipe in (or taking it out of) the home page banner.
/// </summary>
public enum FeatureResult
{
    Updated,
    NotFound,

    /// <summary>The banner shows the recipe's photo, so a recipe without one cannot be featured.</summary>
    NoImage
}
