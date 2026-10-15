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
- **User-facing text** never goes in constants or inline literals. It belongs in resource files (see below).
- **Stored data values** that happen to be Bulgarian (e.g. a seeded category name used in a query) are constants,
  not resources, because translating them would break the lookup (see `IngredientCategoryConstants`).

### Prefer framework values

Use what .NET already provides instead of new constants:
`ClaimTypes.*`, `MediaTypeNames.*` (e.g. `MediaTypeNames.Application.Pdf`), `StatusCodes.*`,
and `nameof(...)` for action names: `RedirectToAction(nameof(Index))`,
`RedirectToAction(nameof(RecipesController.Details), "Recipes", ...)`.
Caveat: `nameof` gives the method name, so it does not match the route for actions with an `Async` suffix
(`HomeController.IndexAsync` routes as `"Index"`); keep the literal there. View names that are not action names
(e.g. `View("FollowList", model)`) may stay literal.

## User-facing text (resources)

Every string a user can see in backend code (C# files) comes from a `.resx` file in `RecipeBook/Resources/`.
No Bulgarian (or any UI) text as string literals in controllers, services, models or view models.

### Which resource file

| File | Contents | Example |
|---|---|---|
| `DisplayTexts` | Field labels and display names | `[Display(Name = nameof(DisplayTexts.Email), ResourceType = typeof(DisplayTexts))]` |
| `ValidationTexts` | Data-annotation error messages | `[Required(ErrorMessageResourceName = nameof(ValidationTexts.FieldRequired), ErrorMessageResourceType = typeof(ValidationTexts))]` |
| `MessageTexts` | Controller/service feedback: TempData and ModelState messages, page titles, emails | `TempData[TempDataKeys.SuccessMessage] = MessageTexts.RecipeDeleted;` |
| `IdentityErrorTexts` | ASP.NET Core Identity errors (via `CustomIdentityErrorDescriber`) | |
| `RecipePdfTexts` | Text in the generated recipe PDF | |

Add a new `<Area>Texts.resx` only when a new area does not fit any of these.

### How to use them

- Use the strongly typed generated classes (`MessageTexts.RecipeDeleted`), not string keys, so typos fail at compile time.
  In attributes, reference keys with `nameof(...)` plus `ResourceType` / `ErrorMessageResourceType` — never `ErrorMessage = "..."`.
- Parameterized text uses numbered placeholders in the resource and `string.Format` in code:
  `string.Format(MessageTexts.IngredientInUse, recipeNames)` — never string interpolation of UI text.
  In validation messages use the attribute's own placeholders instead of repeating limits
  (`{0}` field name; `StringLength`: `{1}` max, `{2}` min; `Range`: `{1}` min, `{2}` max).
- Key names are PascalCase and describe meaning, not wording (`UserDeleted`, not `PotrebitelqtBeshe...`).
  Reuse an existing key when the meaning is the same.
- The neutral `.resx` holds the Bulgarian text (default culture `bg`); translations go in `<Name>.<culture>.resx`.
- Every `.resx` needs its `*.Designer.cs` (`PublicResXFileCodeGenerator`) and both `EmbeddedResource`/`Compile`
  entries in `RecipeBook.csproj`. When adding a key, update the `.resx` and its `Designer.cs` together
  (Visual Studio regenerates the Designer on save).
- Not user-facing, so stays as literals: log messages, exception messages for developers, and route/view names.
- Razor views (`.cshtml`) are not covered by this rule yet.

## Style

- `public const string` (keyword `string`, not `String`); use `public static readonly` only for values that cannot be
  `const` (arrays, `Regex`).
- PascalCase names, no `ALL_CAPS` or prefixes.
- Never put secrets, passwords or connection strings in constants.
- Tests reference the constants instead of repeating the literal values.
