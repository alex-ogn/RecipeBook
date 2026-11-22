using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Constants;
using RecipeBook.Data;
using RecipeBook.Models;

namespace Tests.Integration;

/// <summary>
/// The home page sections and the quick-filter links it sends visitors to.
/// </summary>
public class HomePageIntegrationTests : IClassFixture<RecipeBookFactory>, IAsyncLifetime
{
    private const int VeganRecipeId = 200;
    private const string VeganRecipeTitle = "Веганска салата";
    private const string SeededRecipeTitle = "Боб чорба";

    // Class names from Views/Home/Index.cshtml that mark the signed-out-only sections.
    private const string JoinPanelMarker = "class=\"join-cta\"";

    private readonly RecipeBookFactory _factory;

    public HomePageIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (await context.Recipes.FindAsync(VeganRecipeId) == null)
        {
            context.Recipes.Add(new Recipe
            {
                Id = VeganRecipeId,
                UserId = RecipeBookFactory.AuthorId,
                Title = VeganRecipeTitle,
                Description = "<p>Свежа</p>",
                Instructions = "<p>Нарежете</p>",
                Servings = 2,
                CookingTime = RecipeFilterConstants.QuickCookingMinutes,
                RecipeCategoryId = RecipeBookFactory.CategoryId,
                IsVegan = true,
                IsVegetarian = true
            });
            await context.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task HomePage_ForAnonymous_ShowsTheJoinPanel_AndTheQuickPickLinks()
    {
        var response = await _factory.CreateClientAs(null).GetAsync("/");
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(JoinPanelMarker, html);
        Assert.Contains("href=\"/Recipes?IsVegan=true\"", html);
        Assert.Contains($"href=\"/Recipes?MaxCookingTime={RecipeFilterConstants.QuickCookingMinutes}\"", html);
        Assert.Contains($"href=\"/Recipes?SelectedCategoryId={RecipeBookFactory.CategoryId}\"", html);
    }

    [Fact]
    public async Task HomePage_ForSignedInUser_HidesTheJoinPanel()
    {
        var response = await _factory.CreateClientAs(RecipeBookFactory.StrangerId, UserRoles.User).GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(JoinPanelMarker, await response.Content.ReadAsStringAsync());
    }

    public static TheoryData<string> QuickPickUrls => new()
    {
        "/Recipes?IsVegan=true",
        "/Recipes?IsVegetarian=true",
        $"/Recipes?MaxCookingTime={RecipeFilterConstants.QuickCookingMinutes}"
    };

    [Theory]
    [MemberData(nameof(QuickPickUrls))]
    public async Task QuickPickLinks_FilterTheRecipeList(string url)
    {
        var response = await _factory.CreateClientAs(null).GetAsync(url);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(VeganRecipeTitle, html);
        Assert.DoesNotContain(SeededRecipeTitle, html);     // not vegan, and takes 60 minutes
    }

    [Fact]
    public async Task RecipeList_KeepsTheQuickFilter_InTheFilterForm()
    {
        var html = await _factory.CreateClientAs(null).GetStringAsync("/Recipes?IsVegan=true");

        Assert.Contains("name=\"IsVegan\" value=\"true\"", html);
    }
}
