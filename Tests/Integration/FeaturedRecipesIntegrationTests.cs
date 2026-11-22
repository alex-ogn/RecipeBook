using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;

namespace Tests.Integration;

/// <summary>
/// Admins choose the recipes in the home page banner; everyone else is turned away.
/// </summary>
public class FeaturedRecipesIntegrationTests : IClassFixture<RecipeBookFactory>, IAsyncLifetime
{
    private const int FirstPhotoRecipeId = 300;
    private const int SecondPhotoRecipeId = 301;
    private const string AddUrl = "/FeaturedRecipes/Add";
    private const string RemoveUrl = "/FeaturedRecipes/Remove";

    private readonly RecipeBookFactory _factory;

    public FeaturedRecipesIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var id in new[] { FirstPhotoRecipeId, SecondPhotoRecipeId })
        {
            var recipe = await context.Recipes.FindAsync(id);
            if (recipe == null)
            {
                context.Recipes.Add(new Recipe
                {
                    Id = id,
                    UserId = RecipeBookFactory.AuthorId,
                    Title = $"Рецепта със снимка {id}",
                    Description = "<p>Описание</p>",
                    Instructions = "<p>Стъпка</p>",
                    Servings = 2,
                    CookingTime = 20,
                    RecipeCategoryId = RecipeBookFactory.CategoryId,
                    ImageUrl = $"{ImageConstants.RecipeImagesUrlPrefix}{id}.jpg"
                });
            }
            else
            {
                recipe.FeaturedAt = null;   // every test starts with nothing featured
            }
        }

        await context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string SlideMarker(int recipeId) => $"data-slide-url=\"/Recipes/Details/{recipeId}\"";

    private Task<DateTime?> FeaturedAtAsync(int recipeId)
        => _factory.QueryAsync(db => db.Recipes.Where(r => r.Id == recipeId).Select(r => r.FeaturedAt).SingleAsync());

    private async Task<HttpResponseMessage> PostAsAsync(string? userId, string role, string url, int recipeId)
    {
        var client = _factory.CreateClientAs(userId, role);
        var token = await client.GetAntiforgeryTokenAsync($"/Recipes/Details/{RecipeBookFactory.AuthorRecipeId}");
        return await client.PostFormAsync(url, token, ("id", recipeId.ToString()));
    }

    [Fact]
    public async Task HomePage_WithNothingFeatured_ShowsTheMostLikedRecipesWithAPhoto()
    {
        var html = await _factory.CreateClientAs(null).GetStringAsync("/");

        Assert.Contains(SlideMarker(FirstPhotoRecipeId), html);
        Assert.Contains(SlideMarker(SecondPhotoRecipeId), html);
        Assert.DoesNotContain(SlideMarker(RecipeBookFactory.AuthorRecipeId), html);   // no photo
    }

    [Fact]
    public async Task Admin_CanFeatureARecipe_AndTheBannerThenShowsOnlyFeaturedOnes()
    {
        var response = await PostAsAsync(RecipeBookFactory.AdminId, UserRoles.Admin, AddUrl, SecondPhotoRecipeId);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(await FeaturedAtAsync(SecondPhotoRecipeId));

        var html = await _factory.CreateClientAs(null).GetStringAsync("/");
        Assert.Contains(SlideMarker(SecondPhotoRecipeId), html);
        Assert.DoesNotContain(SlideMarker(FirstPhotoRecipeId), html);

        var removed = await PostAsAsync(RecipeBookFactory.AdminId, UserRoles.Admin, RemoveUrl, SecondPhotoRecipeId);
        Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
        Assert.Null(await FeaturedAtAsync(SecondPhotoRecipeId));
    }

    [Fact]
    public async Task Admin_CannotFeatureARecipeWithoutAPhoto()
    {
        var response = await PostAsAsync(RecipeBookFactory.AdminId, UserRoles.Admin, AddUrl, RecipeBookFactory.AuthorRecipeId);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Null(await FeaturedAtAsync(RecipeBookFactory.AuthorRecipeId));
    }

    [Fact]
    public async Task Admin_FeaturingAMissingRecipe_IsNotFound()
    {
        var response = await PostAsAsync(RecipeBookFactory.AdminId, UserRoles.Admin, AddUrl, recipeId: 99999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task User_EvenTheAuthor_CannotFeatureARecipe()
    {
        var response = await PostAsAsync(RecipeBookFactory.AuthorId, UserRoles.User, AddUrl, FirstPhotoRecipeId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(await FeaturedAtAsync(FirstPhotoRecipeId));
    }

    [Fact]
    public async Task Anonymous_IsSentToLogin_AndNothingChanges()
    {
        var client = _factory.CreateClientAs(null);
        var token = await client.GetAntiforgeryTokenAsync($"/Recipes/Details/{RecipeBookFactory.AuthorRecipeId}");
        var response = await client.PostFormAsync(AddUrl, token, ("id", FirstPhotoRecipeId.ToString()));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location!.OriginalString);
        Assert.Null(await FeaturedAtAsync(FirstPhotoRecipeId));
    }

    [Fact]
    public async Task Admin_WithoutAntiforgeryToken_IsRejected()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.AdminId, UserRoles.Admin);
        var response = await client.PostFormAsync(AddUrl, token: null, ("id", FirstPhotoRecipeId.ToString()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(await FeaturedAtAsync(FirstPhotoRecipeId));
    }

    [Fact]
    public async Task RecipePage_ShowsTheFeatureButton_OnlyToAdmins()
    {
        var url = $"/Recipes/Details/{FirstPhotoRecipeId}";
        var asAdmin = await _factory.CreateClientAs(RecipeBookFactory.AdminId, UserRoles.Admin).GetStringAsync(url);
        var asAuthor = await _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User).GetStringAsync(url);

        Assert.Contains($"action=\"{AddUrl}\"", asAdmin);
        Assert.DoesNotContain(AddUrl, asAuthor);
    }
}
