using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace CookbookTests.Controllers;

public class FileControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FileControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_WithValidImage_ReturnsFileName()
    {
        var content = new MultipartFormDataContent();
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        var byteContent = new ByteArrayContent(imageBytes);
        byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(byteContent, "file", "test.jpg");

        var response = await _client.PostAsync("/cookbook/File/Upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fileName = await response.Content.ReadAsStringAsync();
        fileName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Upload_WithInvalidExtension_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();
        var bytes = new byte[] { 1, 2, 3 };
        var byteContent = new ByteArrayContent(bytes);
        byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(byteContent, "file", "test.txt");

        var response = await _client.PostAsync("/cookbook/File/Upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Download_ExistingFile_ReturnsOk()
    {
        var fileName = "existing.jpg";

        var response = await _client.GetAsync($"/cookbook/File/Download?fileName={fileName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Delete_ExistingFile_ReturnsOk()
    {
        var fileName = "to-delete.jpg";

        var response = await _client.DeleteAsync($"/cookbook/File/Delete?fileName={fileName}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}


