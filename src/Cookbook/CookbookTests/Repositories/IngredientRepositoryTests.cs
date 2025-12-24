using CookbookCommon.DTO;
using CookbookDB;
using CookbookDB.Models;
using CookbookDB.Repositories;
using Microsoft.EntityFrameworkCore;
using IngredientModel = CookbookDB.Models.Ingredient;

namespace CookbookTests.Repositories;

public class IngredientRepositoryTests : IDisposable
{
    private readonly CookbookDbContext _context;
    private readonly IngredientRepository _repository;

    public IngredientRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CookbookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CookbookDbContext(options);
        _repository = new IngredientRepository(_context);
    }

    [Fact]
    public async Task Get_WhenIngredientExists_ReturnsIngredient()
    {
        var ingredient = new IngredientModel
        {
            Name = "Test Ingredient",
            Protein = 10,
            Fats = 5,
            Carbs = 20,
            Calories = 150
        };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        var result = await _repository.Get((int)ingredient.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Ingredient");
        result.Protein.Should().Be(10);
    }

    [Fact]
    public async Task Get_WhenIngredientDoesNotExist_ReturnsNull()
    {
        var result = await _repository.Get(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Search_WhenIngredientsExist_ReturnsMatchingIngredients()
    {
        var ingredient1 = new IngredientModel { Name = "Tomato", Protein = 1, Fats = 0, Carbs = 4, Calories = 20 };
        var ingredient2 = new IngredientModel { Name = "Potato", Protein = 2, Fats = 0, Carbs = 17, Calories = 77 };
        var ingredient3 = new IngredientModel { Name = "Carrot", Protein = 1, Fats = 0, Carbs = 10, Calories = 41 };

        _context.Ingredients.AddRange(ingredient1, ingredient2, ingredient3);
        await _context.SaveChangesAsync();

        var result = await _repository.Search("to");

        result.Should().NotBeNull();
        result!.Length.Should().Be(2);
        result.Should().Contain(i => i.Name == "Tomato");
        result.Should().Contain(i => i.Name == "Potato");
        result.Should().NotContain(i => i.Name == "Carrot");
    }

    [Fact]
    public async Task Search_WhenNoIngredientsMatch_ReturnsEmptyArray()
    {
        var ingredient = new IngredientModel { Name = "Tomato", Protein = 1, Fats = 0, Carbs = 4, Calories = 20 };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        var result = await _repository.Search("NonExistent");

        result.Should().NotBeNull();
        result!.Length.Should().Be(0);
    }

    [Fact]
    public async Task AddAndGetMany_WhenIngredientNamesIsNull_ReturnsEmptyList()
    {
        var result = await _repository.AddAndGetMany(null!);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAndGetMany_WhenIngredientNamesIsEmpty_ReturnsEmptyList()
    {
        var result = await _repository.AddAndGetMany(new List<string>());

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAndGetMany_WhenIngredientsExist_ReturnsExistingIngredients()
    {
        var existingIngredient = new IngredientModel { Name = "Tomato", Protein = 1, Fats = 0, Carbs = 4, Calories = 20 };
        _context.Ingredients.Add(existingIngredient);
        await _context.SaveChangesAsync();

        var result = await _repository.AddAndGetMany(new List<string> { "Tomato" });

        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        result[0].Name.Should().Be("Tomato");
        result[0].Id.Should().Be(existingIngredient.Id);
    }

    [Fact]
    public async Task AddAndGetMany_WhenIngredientsDoNotExist_CreatesAndReturnsNewIngredients()
    {
        var result = await _repository.AddAndGetMany(new List<string> { "NewIngredient1", "NewIngredient2" });

        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.Should().Contain(i => i.Name == "NewIngredient1");
        result.Should().Contain(i => i.Name == "NewIngredient2");

        var savedIngredients = await _context.Ingredients.ToListAsync();
        savedIngredients.Count.Should().Be(2);
    }

    [Fact]
    public async Task AddAndGetMany_WhenMixOfExistingAndNew_ReturnsAllIngredients()
    {
        var existingIngredient = new IngredientModel { Name = "Existing", Protein = 1, Fats = 0, Carbs = 4, Calories = 20 };
        _context.Ingredients.Add(existingIngredient);
        await _context.SaveChangesAsync();

        var result = await _repository.AddAndGetMany(new List<string> { "Existing", "NewIngredient" });

        result.Should().NotBeNull();
        result.Count.Should().Be(2);
        result.Should().Contain(i => i.Name == "Existing");
        result.Should().Contain(i => i.Name == "NewIngredient");
    }

    [Fact]
    public async Task AddIngredientAsync_WhenValidData_CreatesAndReturnsId()
    {
        var ingredientCreate = new IngredientCreate
        {
            Name = "Test Ingredient",
            Protein = 10,
            Fats = 5,
            Carbs = 20,
            Calories = 150
        };

        var id = await _repository.AddIngredientAsync(ingredientCreate);

        id.Should().BeGreaterThan(0);

        var savedIngredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
        savedIngredient.Should().NotBeNull();
        savedIngredient!.Name.Should().Be("Test Ingredient");
        savedIngredient.Protein.Should().Be(10);
        savedIngredient.Fats.Should().Be(5);
        savedIngredient.Carbs.Should().Be(20);
        savedIngredient.Calories.Should().Be(150);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

