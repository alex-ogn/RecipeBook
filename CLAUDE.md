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
  - `CookieConstants` — cookie names and lifetimes

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

## Architecture

- **Controllers never use `ApplicationDbContext`.** All data access goes through services in `RecipeBook/Services/`
  (`IRecipeService`, `IRecipeInteractionService`, `IRecipeCategoryService`, `IIngredientCategoryService`,
  `IIngredientService`, `IFollowService`, `IUserProfileService`, `IStatisticsService`), registered as scoped in `Program.cs`.
  Controllers handle input, permissions (`RecipePermissions`), `ModelState`, view models and redirects. Identity's
  `UserManager` is fine in controllers.
- Extend an existing service when the feature fits it; don't add a generic repository (`DbContext` is already the unit of work).
- Recipe lists use `IRecipeService.GetRecipeCardsAsync(RecipeListFilter, page, pageSize)` — add a filter property
  instead of a new query.
- Deletes that can be blocked by references return `DeleteResult` (`Deleted` / `NotFound` / `InUse` with the names).
- Read-only queries use `AsNoTracking()`; a service method saves everything it changes with a single `SaveChangesAsync`.
- Every EF call is async. Do counting, grouping, scoring and sorting in the query, not after `ToList()`/`AsEnumerable()`.
- Lists load only the columns they show: project with `Select` (recipe cards use `RecipeCardSource.Projection`)
  instead of `Include`-ing whole collections (e.g. all likes) just to count them.
- Lists that can grow are paged: order the query (with an `Id` tie-breaker), call `ToPagedResultAsync(page, pageSize)`
  (`QueryableExtensions`), take an `int page = 1` action parameter, use a page size from `PagingConstants`, and render
  `<partial name="_Pager" model="..." />` — it keeps the current filters in its links.
- In-memory tests don't prove SQL translation: when a query does something non-trivial (aggregates over
  subqueries, string functions, grouping), check it against SQL Server. SQL Server cannot `SUM` over a `COUNT` subquery.
- The `Recipe` table is named `Recipies` in the database (`ToTable` in `ApplicationDbContext`); the DbSet is `Recipes`.
- Pass page data through view-model properties, not `ViewData` (keep `ViewData` for `Title` and small layout values).
- "Go back" redirects use `this.RedirectToLocalReferer(fallbackUrl)`, never `Redirect(Request.Headers.Referer)`.
- Rich text is sanitized by the singleton `IHtmlSanitizer` (`RecipeHtmlSanitizer.Create()`), inside the service.

## Security

- **Ownership**: any action that changes or deletes a recipe must check `RecipePermissions.CanModify(recipe.UserId, User)`
  (author or Admin) and return `Forbid()` otherwise — on both the GET (form) and the POST.
- **Uploads**: images go only through `IImageStorageService` (`Validate`, `SaveRecipeImageAsync`, `DeleteRecipeImage`).
  It enforces the allowed extensions, size limit and file signature from `ImageConstants`. Never build a file path from
  user input, and never trust an image URL posted by a form — take it from the database.
- **Antiforgery** is validated globally (`AutoValidateAntiforgeryTokenAttribute` in `Program.cs`). Use tag-helper forms
  (`asp-action`) so the token is added; any AJAX POST must send the `RequestVerificationToken` header.
- **Errors**: never show `ex.Message` to users. Catch only expected exceptions (`ValidationException`,
  `DbUpdateException`), log them with `ILogger<T>`, and show a `MessageTexts` message.

## Style

- The build must stay warning-free. Nullable warnings are errors (`<WarningsAsErrors>nullable</WarningsAsErrors>` in
  both projects). Initialize non-nullable properties: `string` → `= string.Empty`, EF navigation → `= null!`,
  collections → `= new()`, or mark them `required`. Don't silence a warning with `!` unless the value is guaranteed
  (e.g. the user id inside an `[Authorize]` action).
- Don't change a model property's nullability (`?`) without a migration — it changes the database column.
- Style rules live in `.editorconfig`. New files use file-scoped namespaces; every type has a namespace.
- Views render partials with `<partial name="..." model="..." />`, never `Html.Partial`.
- Keep NuGet packages free of known vulnerabilities: check with
  `dotnet list package --vulnerable --include-transitive`; pin a patched transitive version when needed (see the
  `System.Security.Cryptography.Pkcs` pin in `RecipeBook.csproj`).
- `public const string` (keyword `string`, not `String`); use `public static readonly` only for values that cannot be
  `const` (arrays, `Regex`).
- PascalCase names, no `ALL_CAPS` or prefixes.
- Never put secrets, passwords or connection strings in constants.
- Tests reference the constants instead of repeating the literal values.
