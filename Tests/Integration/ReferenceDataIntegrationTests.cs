using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecipeBook.Constants;
using RecipeBook.Data;

namespace Tests.Integration;

/// <summary>
/// The app must work on a fresh database: the data it depends on is seeded at startup.
/// </summary>
public class ReferenceDataIntegrationTests : IClassFixture<RecipeBookFactory>, IAsyncLifetime
{
    private readonly RecipeBookFactory _factory;

    public ReferenceDataIntegrationTests(RecipeBookFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => _factory.SeedAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Startup_SeedsTheAddedByUserCategory_OnlyOnce()
    {
        // Starting the host already ran the seeder once; running it again must not add a duplicate.
        using (var scope = _factory.Services.CreateScope())
        {
            await DataSeeder.InitializeAsync(scope.ServiceProvider);
        }

        var count = await _factory.QueryAsync(db => db.IngredientCategories
            .CountAsync(c => c.Name == IngredientCategoryConstants.AddedByUserCategoryName));

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreatingARecipeWithANewIngredient_WorksOnAFreshDatabase()
    {
        var client = _factory.CreateClientAs(RecipeBookFactory.AuthorId, UserRoles.User);
        var token = await client.GetAntiforgeryTokenAsync("/Recipes/Create");

        var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("Шафранов ориз"), "Title" },
            { new StringContent("<p>Ароматен ориз</p>"), "Description" },
            { new StringContent("<p>Сварете ориза</p>"), "Instructions" },
            { new StringContent("2"), "Servings" },
            { new StringContent("30"), "CookingTime" },
            { new StringContent(RecipeBookFactory.CategoryId.ToString()), "CategoryId" },
            { new StringContent("[{\"IngredientId\":0,\"Name\":\"Шафран\",\"QuantityNeeded\":\"щипка\",\"IsNew\":true}]"), "SelectedIngredientsJson" }
        };

        var response = await client.PostAsync("/Recipes/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var saved = await _factory.QueryAsync(db => db.Recipes
            .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                    .ThenInclude(i => i.IngredientCategory)
            .SingleAsync(r => r.Title == "Шафранов ориз"));

        var ingredient = Assert.Single(saved.RecipeIngredients).Ingredient;
        Assert.Equal("Шафран", ingredient.Name);
        Assert.Equal(IngredientCategoryConstants.AddedByUserCategoryName, ingredient.IngredientCategory!.Name);
    }
}
