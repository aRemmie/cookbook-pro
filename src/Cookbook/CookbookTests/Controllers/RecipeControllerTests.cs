using System.Net;
using System.Net.Http.Json;
using CookbookCommon.DTO;

namespace CookbookTests.Controllers;

public class RecipeControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RecipeControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<RecipeCreate> CreateSampleRecipeAsync(HttpClient client, string name = "Test Recipe")
    {
        var ingredientCreate = new IngredientCreate
        {
            Name = "Tomato",
            Protein = 1,
            Fats = 0,
            Carbs = 4,
            Calories = 20
        };
        var ingredientResponse = await client.PostAsJsonAsync("/cookbook/Ingredient/Create", ingredientCreate);
        ingredientResponse.EnsureSuccessStatusCode();
        var ingredientId = await ingredientResponse.Content.ReadFromJsonAsync<long>();

        return new RecipeCreate
        {
            Name = name,
            Instruction = "Test instruction",
            ServingsNumber = 2,
            Ingredients = new[]
            {
                new RecipeIngredient
                {
                    IngredientId = ingredientId,
                    Weight = 100
                }
            }
        };
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsCreatedRecipe()
    {
        var recipeCreate = await CreateSampleRecipeAsync(_client, "Integration Recipe");

        var createResponse = await _client.PostAsJsonAsync("/cookbook/Recipe/Create", recipeCreate);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdId = await createResponse.Content.ReadFromJsonAsync<long>();
        createdId.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/cookbook/Recipe/Get?id={createdId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var recipe = await getResponse.Content.ReadFromJsonAsync<Recipe>();
        recipe.Should().NotBeNull();
        recipe!.Name.Should().Be("Integration Recipe");
        recipe.ServingsNumber.Should().Be(2);
        recipe.Ingredients.Should().HaveCount(1);
    }

    [Fact]
    public async Task Search_ReturnsMatchingRecipes()
    {
        var recipe1 = await CreateSampleRecipeAsync(_client, "Tomato Soup");
        var recipe2 = await CreateSampleRecipeAsync(_client, "Potato Soup");
        var recipe3 = await CreateSampleRecipeAsync(_client, "Carrot Salad");

        var recipesToCreate = new[] { recipe1, recipe2, recipe3 };

        foreach (var recipeCreate in recipesToCreate)
        {
            var response = await _client.PostAsJsonAsync("/cookbook/Recipe/Create", recipeCreate);
            response.EnsureSuccessStatusCode();
        }

        var searchResponse = await _client.GetAsync("/cookbook/Recipe/Search?name=Soup");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var recipes = await searchResponse.Content.ReadFromJsonAsync<Recipe[]>();

        recipes.Should().NotBeNull();
        recipes!.Length.Should().Be(2);
        recipes.Should().Contain(r => r.Name == "Tomato Soup");
        recipes.Should().Contain(r => r.Name == "Potato Soup");
        recipes.Should().NotContain(r => r.Name == "Carrot Salad");
    }

    [Fact]
    public async Task Update_ChangesRecipeData()
    {
        var recipeCreate = await CreateSampleRecipeAsync(_client, "Original Recipe");
        var createResponse = await _client.PostAsJsonAsync("/cookbook/Recipe/Create", recipeCreate);
        createResponse.EnsureSuccessStatusCode();
        var id = await createResponse.Content.ReadFromJsonAsync<long>();

        var update = new RecipeUpdate
        {
            Id = id,
            Name = "Updated Recipe",
            Instruction = "Updated instruction",
            ServingsNumber = 4,
            Ingredients = recipeCreate.Ingredients
        };

        var updateResponse = await _client.PostAsJsonAsync("/cookbook/Recipe/Update", update);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/cookbook/Recipe/Get?id={id}");
        var recipe = await getResponse.Content.ReadFromJsonAsync<Recipe>();

        recipe.Should().NotBeNull();
        recipe!.Name.Should().Be("Updated Recipe");
        recipe.Instruction.Should().Be("Updated instruction");
        recipe.ServingsNumber.Should().Be(4);
    }

    [Fact]
    public async Task Delete_RemovesRecipe()
    {
        var recipeCreate = await CreateSampleRecipeAsync(_client, "Recipe To Delete");
        var createResponse = await _client.PostAsJsonAsync("/cookbook/Recipe/Create", recipeCreate);
        createResponse.EnsureSuccessStatusCode();
        var id = await createResponse.Content.ReadFromJsonAsync<long>();

        var deleteResponse = await _client.DeleteAsync($"/cookbook/Recipe/Delete?id={id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/cookbook/Recipe/Get?id={id}");

        // ASP.NET Core returns 204 NoContent when a non-nullable return type is null
        getResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetRandomFromTheMealDB_ReturnsFakeRecipe()
    {
        var response = await _client.GetAsync("/cookbook/Recipe/GetRandomFromTheMealDB");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recipe = await response.Content.ReadFromJsonAsync<Recipe>();

        recipe.Should().NotBeNull();
        recipe!.Name.Should().Be("Test MealDB Recipe");
    }
}


