using CookbookCommon.DTO;
using CookbookFileStorage;
using Microsoft.EntityFrameworkCore;
using Recipe = CookbookDB.Models.Recipe;

namespace CookbookDB.Repositories;
public class RecipeRepository(CookbookDbContext context, IFileService fileService)
{
    private readonly CookbookDbContext _context = context;
    private readonly IFileService _fileService = fileService;

    public async Task<Recipe?> Get(int id)
    {
        return await _context.Recipes
            .Include(r => r.User)
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Recipe[]?> Search(string name)
    {
        return await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .Where(r => r.Name.Contains(name))
            .ToArrayAsync();
    }

    public async Task<long> AddRecipeAsync(RecipeCreate recipeCreate, long userId)
    {
        var ingredientIds = recipeCreate.Ingredients.Select(i => i.IngredientId!.Value);
        var existingIngredients = _context.Ingredients.Where(i => ingredientIds.Contains(i.Id));

        var notFoundIngredients = ingredientIds.Except(existingIngredients.Select(i => i.Id));

        if (notFoundIngredients.Any())
        {
            throw new Exception("Не найден(ы) ингредиент(ы) по идентификатору: " +
                string.Join(", ", notFoundIngredients.Select(i => i.ToString())));
        }

        var duplicates = ingredientIds.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key);

        if (duplicates.Any())
        {
            throw new Exception("Дублирование ингредиента(ов) по идентификатору: " +
               string.Join(", ", duplicates.Select(i => i.ToString())));
        }

        var recipe = new Recipe
        {
            Name = recipeCreate.Name,
            Instruction = recipeCreate.Instruction,
            ServingsNumber = recipeCreate.ServingsNumber,
            FileName = recipeCreate.FileName,
            UserId = userId,
        };

        await _context.Recipes.AddAsync(recipe);

        var id = recipe.Id;
        recipe.RecipeIngredients = recipeCreate.Ingredients
            .Select(i => new Models.RecipeIngredient
            {
                RecipeId = id,
                IngredientId = i.IngredientId!.Value,
                Weight = i.Weight,
            }).ToArray();

        await _context.SaveChangesAsync();

        return recipe.Id;
    }

    public async Task UpdateRecipeAsync(RecipeUpdate recipeUpdate, long userId)
    {
        var existingRecipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == recipeUpdate.Id);

        if (existingRecipe == null)
        {
            throw new Exception($"Не найден рецепт по идентификатору {recipeUpdate.Id}");
        }

        if (existingRecipe.UserId != userId)
        {
            throw new Exception($"Нельзя изменить рецепт, созданный другим пользователем");
        }

        var ingredientIds = recipeUpdate.Ingredients.Select(i => i.IngredientId!.Value);
        var existingIngredients = _context.Ingredients.Where(i => ingredientIds.Contains(i.Id));

        var notFoundIngredients = ingredientIds.Except(existingIngredients.Select(i => i.Id));

        if (notFoundIngredients.Any())
        {
            throw new Exception("Не найден(ы) ингредиент(ы) по идентификатору: " +
                string.Join(", ", notFoundIngredients.Select(i => i.ToString())));
        }

        var duplicates = ingredientIds.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key);

        if (duplicates.Any())
        {
            throw new Exception("Дублирование ингредиента(ов) по идентификатору: " +
               string.Join(", ", duplicates.Select(i => i.ToString())));
        }


        if (!string.IsNullOrEmpty(existingRecipe.FileName) && existingRecipe.FileName != recipeUpdate.FileName)
        {
            await _fileService.DeleteFileAsync(existingRecipe.FileName);
        }

        existingRecipe.Name = recipeUpdate.Name;
        existingRecipe.Instruction = recipeUpdate.Instruction;
        existingRecipe.ServingsNumber = recipeUpdate.ServingsNumber;
        existingRecipe.FileName = recipeUpdate.FileName;

        existingRecipe.RecipeIngredients = recipeUpdate.Ingredients
            .Select(i => new Models.RecipeIngredient
            {
                RecipeId = recipeUpdate.Id,
                IngredientId = i.IngredientId!.Value,
                Weight = i.Weight,
            }).ToList();

        await _context.SaveChangesAsync();
    }

    public async Task DeleteRecipeAsync(long id, long userId)
    {
        var existingRecipe = await _context.Recipes
            .Include(r => r.RecipeIngredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (existingRecipe == null)
        {
            throw new Exception($"Не найден рецепт по идентификатору {id}");
        }

        if (existingRecipe.UserId != userId)
        {
            throw new Exception($"Нельзя удалить рецепт, созданный другим пользователем");
        }

        if (!string.IsNullOrEmpty(existingRecipe.FileName))
        {
            await _fileService.DeleteFileAsync(existingRecipe.FileName);
        }

        _context.RecipeIngredients.RemoveRange(existingRecipe.RecipeIngredients);
        _context.Recipes.Remove(existingRecipe);
        await _context.SaveChangesAsync();
    }
}