using CookbookCommon.DTO;
using CookbookDB;
using CookbookDB.Models;
using CookbookDB.Repositories;
using CookbookFileStorage;
using Microsoft.EntityFrameworkCore;
using Moq;
using IngredientModel = CookbookDB.Models.Ingredient;
using RecipeModel = CookbookDB.Models.Recipe;
using RecipeIngredientModel = CookbookDB.Models.RecipeIngredient;
using RecipeIngredientDTO = CookbookCommon.DTO.RecipeIngredient;

namespace CookbookTests.Repositories;

public class RecipeRepositoryTests : IDisposable
{
    private readonly CookbookDbContext _context;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly RecipeRepository _repository;

    public RecipeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CookbookDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CookbookDbContext(options);
        _fileServiceMock = new Mock<IFileService>();
        _repository = new RecipeRepository(_context, _fileServiceMock.Object);
    }

    [Fact]
    public async Task Get_WhenRecipeExists_ReturnsRecipeWithIngredients()
    {
        var ingredient = new IngredientModel { Name = "Test Ingredient", Protein = 10, Fats = 5, Carbs = 20, Calories = 150 };
        var recipe = new RecipeModel
        {
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 4,
            FileName = "test.jpg"
        };
        var recipeIngredient = new RecipeIngredientModel
        {
            Recipe = recipe,
            Ingredient = ingredient,
            Weight = 100
        };

        _context.Ingredients.Add(ingredient);
        _context.Recipes.Add(recipe);
        _context.RecipeIngredients.Add(recipeIngredient);
        await _context.SaveChangesAsync();

        var result = await _repository.Get((int)recipe.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Recipe");
        result.RecipeIngredients.Should().NotBeNull();
        result.RecipeIngredients.Count.Should().Be(1);
        result.RecipeIngredients.First().Ingredient.Name.Should().Be("Test Ingredient");
    }

    [Fact]
    public async Task Get_WhenRecipeDoesNotExist_ReturnsNull()
    {
        var result = await _repository.Get(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Search_WhenRecipesExist_ReturnsMatchingRecipes()
    {
        var recipe1 = new RecipeModel { Name = "Tomato Soup", Instruction = "Cook", ServingsNumber = 2 };
        var recipe2 = new RecipeModel { Name = "Potato Salad", Instruction = "Mix", ServingsNumber = 4 };
        var recipe3 = new RecipeModel { Name = "Carrot Cake", Instruction = "Bake", ServingsNumber = 8 };

        _context.Recipes.AddRange(recipe1, recipe2, recipe3);
        await _context.SaveChangesAsync();

        var result = await _repository.Search("to");

        result.Should().NotBeNull();
        result!.Length.Should().Be(2);
        result.Should().Contain(r => r.Name == "Tomato Soup");
        result.Should().Contain(r => r.Name == "Potato Salad");
        result.Should().NotContain(r => r.Name == "Carrot Cake");
    }

    [Fact]
    public async Task AddRecipeAsync_WhenValidData_CreatesRecipe()
    {
        var ingredient = new IngredientModel { Name = "Test Ingredient", Protein = 10, Fats = 5, Carbs = 20, Calories = 150 };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        var recipeCreate = new RecipeCreate
        {
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 4,
            Ingredients = new List<RecipeIngredientDTO>
            {
                new RecipeIngredientDTO { IngredientId = ingredient.Id, Weight = 100 }
            },
            FileName = "test.jpg"
        };

        var id = await _repository.AddRecipeAsync(recipeCreate);

        id.Should().BeGreaterThan(0);

        var savedRecipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == id);

        savedRecipe.Should().NotBeNull();
        savedRecipe!.Name.Should().Be("Test Recipe");
        savedRecipe.Instruction.Should().Be("Test Instruction");
        savedRecipe.ServingsNumber.Should().Be(4);
        savedRecipe.FileName.Should().Be("test.jpg");
        savedRecipe.RecipeIngredients.Count.Should().Be(1);
        savedRecipe.RecipeIngredients.First().IngredientId.Should().Be(ingredient.Id);
        savedRecipe.RecipeIngredients.First().Weight.Should().Be(100);
    }

    [Fact]
    public async Task AddRecipeAsync_WhenIngredientNotFound_ThrowsException()
    {
        var recipeCreate = new RecipeCreate
        {
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 4,
            Ingredients = new List<RecipeIngredientDTO>
            {
                new RecipeIngredientDTO { IngredientId = 999, Weight = 100 }
            }
        };

        await Assert.ThrowsAsync<Exception>(() => _repository.AddRecipeAsync(recipeCreate));
    }

    [Fact]
    public async Task AddRecipeAsync_WhenDuplicateIngredients_ThrowsException()
    {
        var ingredient = new IngredientModel { Name = "Test Ingredient", Protein = 10, Fats = 5, Carbs = 20, Calories = 150 };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        var recipeCreate = new RecipeCreate
        {
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 4,
            Ingredients = new List<RecipeIngredientDTO>
            {
                new RecipeIngredientDTO { IngredientId = ingredient.Id, Weight = 100 },
                new RecipeIngredientDTO { IngredientId = ingredient.Id, Weight = 200 }
            }
        };

        var exception = await Assert.ThrowsAsync<Exception>(() => _repository.AddRecipeAsync(recipeCreate));
        exception.Message.Should().Contain("Дублирование ингредиента");
    }

    [Fact]
    public async Task UpdateRecipeAsync_WhenRecipeExists_UpdatesRecipe()
    {
        var ingredient1 = new IngredientModel { Name = "Ingredient 1", Protein = 10, Fats = 5, Carbs = 20, Calories = 150 };
        var ingredient2 = new IngredientModel { Name = "Ingredient 2", Protein = 5, Fats = 2, Carbs = 10, Calories = 75 };
        _context.Ingredients.AddRange(ingredient1, ingredient2);
        await _context.SaveChangesAsync();

        var recipe = new RecipeModel
        {
            Name = "Original Recipe",
            Instruction = "Original Instruction",
            ServingsNumber = 2,
            FileName = "original.jpg"
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        var recipeUpdate = new RecipeUpdate
        {
            Id = recipe.Id,
            Name = "Updated Recipe",
            Instruction = "Updated Instruction",
            ServingsNumber = 4,
            Ingredients = new List<RecipeIngredientDTO>
            {
                new RecipeIngredientDTO { IngredientId = ingredient2.Id, Weight = 200 }
            },
            FileName = "updated.jpg"
        };

        await _repository.UpdateRecipeAsync(recipeUpdate);

        var updatedRecipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .FirstOrDefaultAsync(r => r.Id == recipe.Id);

        updatedRecipe.Should().NotBeNull();
        updatedRecipe!.Name.Should().Be("Updated Recipe");
        updatedRecipe.Instruction.Should().Be("Updated Instruction");
        updatedRecipe.ServingsNumber.Should().Be(4);
        updatedRecipe.FileName.Should().Be("updated.jpg");
        updatedRecipe.RecipeIngredients.Count.Should().Be(1);
        updatedRecipe.RecipeIngredients.First().IngredientId.Should().Be(ingredient2.Id);
    }

    [Fact]
    public async Task UpdateRecipeAsync_WhenRecipeDoesNotExist_ThrowsException()
    {
        var recipeUpdate = new RecipeUpdate
        {
            Id = 999,
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 4,
            Ingredients = new List<RecipeIngredientDTO>()
        };

        var exception = await Assert.ThrowsAsync<Exception>(() => _repository.UpdateRecipeAsync(recipeUpdate));
        exception.Message.Should().Contain("Не найден рецепт");
    }

    [Fact]
    public async Task UpdateRecipeAsync_WhenFileNameChanges_DeletesOldFile()
    {
        var recipe = new RecipeModel
        {
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 2,
            FileName = "old.jpg"
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        var recipeUpdate = new RecipeUpdate
        {
            Id = recipe.Id,
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 2,
            Ingredients = new List<RecipeIngredientDTO>(),
            FileName = "new.jpg"
        };

        await _repository.UpdateRecipeAsync(recipeUpdate);

        _fileServiceMock.Verify(f => f.DeleteFileAsync("old.jpg"), Times.Once);
    }

    [Fact]
    public async Task DeleteRecipeAsync_WhenRecipeExists_DeletesRecipe()
    {
        var recipe = new RecipeModel
        {
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 2,
            FileName = "test.jpg"
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        await _repository.DeleteRecipeAsync(recipe.Id);

        var deletedRecipe = await _context.Recipes.FindAsync(recipe.Id);
        deletedRecipe.Should().BeNull();
        _fileServiceMock.Verify(f => f.DeleteFileAsync("test.jpg"), Times.Once);
    }

    [Fact]
    public async Task DeleteRecipeAsync_WhenRecipeDoesNotExist_ThrowsException()
    {
        var exception = await Assert.ThrowsAsync<Exception>(() => _repository.DeleteRecipeAsync(999));
        exception.Message.Should().Contain("Не найден рецепт");
    }

    [Fact]
    public async Task DeleteRecipeAsync_WhenRecipeHasNoFileName_DoesNotCallDeleteFile()
    {
        var recipe = new RecipeModel
        {
            Name = "Test Recipe",
            Instruction = "Test Instruction",
            ServingsNumber = 2,
            FileName = null
        };
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();

        await _repository.DeleteRecipeAsync(recipe.Id);

        _fileServiceMock.Verify(f => f.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

