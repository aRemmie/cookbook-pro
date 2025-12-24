using CookbookFileStorage;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel.Args;
using Moq;
using System.Text;

namespace CookbookTests.Services;

public class MinioServiceTests
{
    private readonly Mock<IMinioClient> _minioClientMock;
    private readonly CookbookFileStorage.MinioConfig _config;
    private readonly MinioService _service;

    public MinioServiceTests()
    {
        _minioClientMock = new Mock<IMinioClient>();
        _config = new CookbookFileStorage.MinioConfig
        {
            BucketName = "test-bucket",
            PublicUrl = "http://localhost:9000"
        };
        _service = new MinioService(_minioClientMock.Object, _config);
    }

    [Fact]
    public async Task BucketExistsAsync_WhenBucketExists_ReturnsTrue()
    {
        _minioClientMock
            .Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.BucketExistsAsync("test-bucket");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task BucketExistsAsync_WhenBucketDoesNotExist_ReturnsFalse()
    {
        _minioClientMock
            .Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.BucketExistsAsync("test-bucket");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateBucketIfNotExistsAsync_WhenBucketDoesNotExist_CreatesBucket()
    {
        _minioClientMock
            .Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _minioClientMock
            .Setup(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _minioClientMock
            .Setup(c => c.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.CreateBucketIfNotExistsAsync("test-bucket");

        _minioClientMock.Verify(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Once);
        _minioClientMock.Verify(c => c.SetPolicyAsync(It.IsAny<SetPolicyArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBucketIfNotExistsAsync_WhenBucketExists_DoesNotCreateBucket()
    {
        _minioClientMock
            .Setup(c => c.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.CreateBucketIfNotExistsAsync("test-bucket");

        _minioClientMock.Verify(c => c.MakeBucketAsync(It.IsAny<MakeBucketArgs>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Note: UploadFileAsync, DownloadFileAsync tests are skipped due to complexity of Minio API mocking
    // These would require more complex setup with Minio response types

    [Fact]
    public async Task DeleteFileAsync_WhenFileExists_DeletesFile()
    {
        _minioClientMock
            .Setup(c => c.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _service.DeleteFileAsync("test.jpg");

        _minioClientMock.Verify(c => c.RemoveObjectAsync(It.IsAny<RemoveObjectArgs>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Note: GetFileUrlAsync test is skipped due to Minio API signature complexity

    [Fact]
    public void GetPublicUrl_WhenValidFileName_ReturnsPublicUrl()
    {
        var result = _service.GetPublicUrl("test.jpg");

        result.Should().Be("http://localhost:9000/test-bucket/test.jpg");
    }

    [Fact]
    public void GetPublicUrl_WhenPublicUrlHasTrailingSlash_RemovesIt()
    {
        var configWithSlash = new CookbookFileStorage.MinioConfig
        {
            BucketName = "test-bucket",
            PublicUrl = "http://localhost:9000/"
        };
        var serviceWithSlash = new MinioService(_minioClientMock.Object, configWithSlash);

        var result = serviceWithSlash.GetPublicUrl("test.jpg");

        result.Should().Be("http://localhost:9000/test-bucket/test.jpg");
    }
}

