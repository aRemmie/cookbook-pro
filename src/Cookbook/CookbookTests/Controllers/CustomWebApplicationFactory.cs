using System.Linq;
using CookbookCommon.DTO;
using CookbookDB;
using CookbookFileStorage;
using CookbookTheMealDB;
using CookbookWebApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CookbookTests.Controllers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CookbookDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(CookbookDbContext));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var databaseName = $"CookbookTestsDb_{Guid.NewGuid()}";

            services.AddScoped<CookbookDbContext>(serviceProvider =>
            {
                var options = new DbContextOptionsBuilder<CookbookDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options;
                
                return new CookbookDbContext(options);
            });

            services.RemoveAll<IFileService>();
            services.AddSingleton<IFileService, TestFileService>();

            if (!services.Any(s => s.ServiceType == typeof(MinioConfig)))
            {
                services.AddSingleton(new MinioConfig
                {
                    BucketName = "test-bucket",
                    PublicUrl = "http://fake-url"
                });
            }

            services.RemoveAll<IMealDBService>();
            services.AddSingleton<IMealDBService, TestMealDbService>();
        });
    }
}

internal class TestFileService : IFileService
{
    public Task<bool> BucketExistsAsync(string bucketName) => Task.FromResult(true);

    public Task CreateBucketIfNotExistsAsync(string bucketName) => Task.CompletedTask;

    public Task<string> UploadFileAsync(IFormFile file, string fileName)
    {
        return Task.FromResult(fileName ?? file.FileName);
    }

    public Task<Stream> DownloadFileAsync(string fileName)
    {
        Stream stream = new MemoryStream();
        return Task.FromResult(stream);
    }

    public Task<List<string>> ListFilesAsync(string prefix)
    {
        var list = new List<string>();
        return Task.FromResult(list);
    }

    public Task DeleteFileAsync(string fileName)
    {
        return Task.CompletedTask;
    }

    public Task<string> GetFileUrlAsync(string fileName, int expirySeconds = 3600)
    {
        return Task.FromResult($"http://fake-url/{fileName}");
    }

    public string GetPublicUrl(string fileName)
    {
        return $"http://fake-url/{fileName}";
    }
}

internal class TestMealDbService : IMealDBService
{
    public Task<Recipe?> GetRandomRecipeAsync()
    {
        var recipe = new Recipe
        {
            Name = "Test MealDB Recipe",
            Instruction = "Test instruction",
            ServingsNumber = 2,
            Weight = 500,
            Ingredients = new List<Ingredient>()
        };

        return Task.FromResult<Recipe?>(recipe);
    }
}


