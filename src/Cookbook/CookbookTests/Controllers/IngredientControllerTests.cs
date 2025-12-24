using System.Net;
using System.Net.Http.Json;
using CookbookCommon.DTO;
using CookbookTests.Controllers;

namespace CookbookTests.Controllers;

public class IngredientControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IngredientControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Then_Get_ReturnsCreatedIngredient()
    {
        var ingredientCreate = new IngredientCreate
        {
            Name = "Integration Ingredient",
            Protein = 10,
            Fats = 5,
            Carbs = 20,
            Calories = 150
        };

        var createResponse = await _client.PostAsJsonAsync("/cookbook/Ingredient/Create", ingredientCreate);

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdId = await createResponse.Content.ReadFromJsonAsync<long>();
        createdId.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/cookbook/Ingredient/Get?id={createdId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ingredient = await getResponse.Content.ReadFromJsonAsync<IngredientBase>();
        ingredient.Should().NotBeNull();
        ingredient!.Name.Should().Be("Integration Ingredient");
        ingredient.Protein.Should().Be(10);
        ingredient.Fats.Should().Be(5);
        ingredient.Carbs.Should().Be(20);
        ingredient.Calories.Should().Be(150);
    }

    [Fact]
    public async Task Search_ReturnsMatchingIngredients()
    {
        var toCreate = new[]
        {
            new IngredientCreate { Name = "Tomato", Protein = 1, Fats = 0, Carbs = 4, Calories = 20 },
            new IngredientCreate { Name = "Potato", Protein = 2, Fats = 0, Carbs = 17, Calories = 77 },
            new IngredientCreate { Name = "Carrot", Protein = 1, Fats = 0, Carbs = 10, Calories = 41 }
        };

        foreach (var ingredientCreate in toCreate)
        {
            var response = await _client.PostAsJsonAsync("/cookbook/Ingredient/Create", ingredientCreate);
            response.EnsureSuccessStatusCode();
        }

        var searchResponse = await _client.GetAsync("/cookbook/Ingredient/Search?name=to");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var ingredients = await searchResponse.Content.ReadFromJsonAsync<IngredientBase[]>();

        ingredients.Should().NotBeNull();
        ingredients!.Length.Should().Be(2);
        ingredients.Should().Contain(i => i.Name == "Tomato");
        ingredients.Should().Contain(i => i.Name == "Potato");
        ingredients.Should().NotContain(i => i.Name == "Carrot");
    }
}


