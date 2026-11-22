namespace RecipeBook.ViewModels.Home;

/// <summary>
/// One category tile on the home page: its name, how many recipes it has, and the photo of its
/// most liked recipe that has one (null when none of its recipes has a photo).
/// </summary>
public record CategoryTileViewModel(int Id, string Name, int RecipeCount, string? ImageUrl);
