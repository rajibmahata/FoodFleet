using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FoodFleet.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FoodFleet.Infrastructure.Services;

public class AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger) : IFileStorageService
{
    private readonly string _connectionString = configuration["Azure:BlobStorage:ConnectionString"]
        ?? throw new InvalidOperationException("Azure:BlobStorage:ConnectionString is not configured.");
    private readonly string _containerName = configuration["Azure:BlobStorage:ContainerName"] ?? "foodfleet";

    private BlobContainerClient GetContainer()
    {
        var serviceClient = new BlobServiceClient(_connectionString);
        return serviceClient.GetBlobContainerClient(_containerName);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var container = GetContainer();
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        // Namespace by date + UUID for uniqueness/security (no user-controlled path traversal)
        var blobName = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
        var blobClient = container.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(fileStream, uploadOptions, ct);
        logger.LogInformation("Uploaded blob: {BlobName}", blobName);
        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var blobName = string.Join("/", uri.Segments.Skip(2)); // skip host + container
            var container = GetContainer();
            var blobClient = container.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
            logger.LogInformation("Deleted blob: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob: {FileUrl}", fileUrl);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove path traversal and restrict to safe characters
        var name = Path.GetFileName(fileName);
        return string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_'));
    }
}
