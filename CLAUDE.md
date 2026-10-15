# RecipeBook

ASP.NET Core MVC app (.NET 10, EF Core + SQL Server, ASP.NET Core Identity). Tests are xUnit in `Tests/`.

- Build: `dotnet build`
- Test: `dotnet test`
- Secrets (email, admin credentials) come from user secrets / environment variables, never from source.

## Constants and magic strings

Do not hard-code strings or numbers that carry meaning across the app. This covers:
role names, TempData/ViewData keys, configuration sections and keys, file-system folders and URL prefixes,
content types, default/fallback values, regex patterns, and cache/session keys.

Exceptions: `ViewData["Title"]` (Razor convention) and user-facing text (see below).

### Where constants go

- **Shared values** → `RecipeBook/Constants/`, namespace `RecipeBook.Constants`, one `public static class` per domain:
  - `UserRoles` — role names (`UserRoles.Admin`, `UserRoles.User`)
  - `TempDataKeys` / `ViewDataKeys` — keys passed between controllers and views
  - `ImageConstants` — image folders, URL prefixes, default images
  - `UserProfileConstants` — user/profile defaults and validation values

  Add to an existing class when the value fits its domain; otherwise create a new `<Domain>Constants` class.
  `RecipeBook.Constants` is imported in `Views/_ViewImports.cshtml`, so views use the same constants.
- **Values used by a single class** that are not part of any contract → `private const` in that class
  (e.g. `FeaturedRecipesCount` in `HomeController`).
- **Configuration** → a strongly typed options class in `RecipeBook/Options/` with `public const string SectionName`,
  registered in `Program.cs` with `builder.Services.Configure<T>(builder.Configuration.GetSection(T.SectionName))`
  and injected as `IOptions<T>`. Never read `_configuration["Section:Key"]` directly.
- **User-facing text** stays out of constants. Existing inline Bulgarian messages may stay; new localizable text
  belongs in `RecipeBook/Resources/*.resx` (see `RecipePdfTexts`, `IdentityErrorTexts`).

### Prefer framework values

Use what .NET already provides instead of new constants:
`ClaimTypes.*`, `MediaTypeNames.*` (e.g. `MediaTypeNames.Application.Pdf`), `StatusCodes.*`,
and `nameof(...)` for action names: `RedirectToAction(nameof(Index))`,
`RedirectToAction(nameof(RecipesController.Details), "Recipes", ...)`.
Caveat: `nameof` gives the method name, so it does not match the route for actions with an `Async` suffix
(`HomeController.IndexAsync` routes as `"Index"`); keep the literal there. View names that are not action names
(e.g. `View("FollowList", model)`) may stay literal.

### Style

- `public const string` (keyword `string`, not `String`); use `public static readonly` only for values that cannot be
  `const` (arrays, `Regex`).
- PascalCase names, no `ALL_CAPS` or prefixes.
- Never put secrets, passwords or connection strings in constants.
- Tests reference the constants instead of repeating the literal values.
