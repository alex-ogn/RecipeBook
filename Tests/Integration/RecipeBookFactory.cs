using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RecipeBook.Data;
using RecipeBook.Models;
using RecipeBook.Services;

namespace Tests.Integration;

/// <summary>
/// Hosts the real app (routing, filters, authorization, antiforgery, views) on an in-memory database
/// with header-based test authentication. Each factory instance gets its own database.
/// </summary>
public class RecipeBookFactory : WebApplicationFactory<Program>
{
    public const string AuthorId = "author";
    public const string StrangerId = "stranger";
    public const string AdminId = "admin";
    public const int AuthorRecipeId = 100;
    public const int CategoryId = 1;

    private readonly string _databaseName = $"RecipeBookIntegration-{Guid.NewGuid()}";

    /// <summary>
    /// Every email the app sent (nothing leaves the test).
    /// </summary>
    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server with an in-memory database (EF Core 9+ also needs the options configuration removed).
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Identity sets its cookie scheme as the default; make the test scheme handle everything.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultForbidScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    /// <summary>
    /// Adds two users, an admin, a category and one recipe by <see cref="AuthorId"/>.
    /// </summary>
    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (await context.Recipes.AnyAsync(r => r.Id == AuthorRecipeId))
        {
            return;
        }

        context.Users.AddRange(
            new ApplicationUser { Id = AuthorId, UserName = AuthorId, Email = "author@test.local" },
            new ApplicationUser { Id = StrangerId, UserName = StrangerId, Email = "stranger@test.local" },
            new ApplicationUser { Id = AdminId, UserName = AdminId, Email = "admin@test.local" });
        // The "added by user" ingredient category is not added here: the app's DataSeeder creates it on startup.
        context.RecipeCategories.Add(new RecipeCategory { Id = CategoryId, Name = "Супи" });
        context.Recipes.Add(new Recipe
        {
            Id = AuthorRecipeId,
            UserId = AuthorId,
            Title = "Боб чорба",
            Description = "<p>Описание</p>",
            Instructions = "<p>Сварете</p>",
            Servings = 4,
            CookingTime = 60,
            RecipeCategoryId = CategoryId
        });

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a real Identity user with a password, for tests that go through the login form.
    /// </summary>
    public async Task<ApplicationUser> CreateLoginUserAsync(string userName, string password, bool emailConfirmed = true)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = userName, Email = $"{userName}@test.local", EmailConfirmed = emailConfirmed };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    public async Task AddToRoleAsync(string userName, string role)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(userName)
            ?? throw new InvalidOperationException($"No user {userName}.");
        await userManager.AddToRoleAsync(user, role);
    }

    /// <summary>
    /// The user as currently stored (fresh scope, so no stale tracked entity), or null when deleted.
    /// </summary>
    public async Task<ApplicationUser?> FindUserAsync(string userId)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(userId);
    }

    public async Task<bool> HasPasswordAsync(string userId, string password)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        return user != null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> IsLockedOutAsync(string userName)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(userName);
        return user != null && await userManager.IsLockedOutAsync(user);
    }

    public async Task<T> QueryAsync<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }
}
