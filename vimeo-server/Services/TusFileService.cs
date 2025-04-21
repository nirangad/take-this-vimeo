using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;
using System.Text;

namespace vimeo_server.Services;

public class TusFileService
{
    private readonly string _videosPath;
    private readonly string _tusPath;
    private readonly IConfiguration _configuration;
    private const string TusUrlPath = "/videos";

    public TusFileService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _videosPath = Path.Combine(environment.ContentRootPath, "videos");
        _tusPath = Path.Combine(_videosPath);
        _configuration = configuration;

        // Create directories if they don't exist
        if (!Directory.Exists(_videosPath))
        {
            Directory.CreateDirectory(_videosPath);
        }
        if (!Directory.Exists(_tusPath))
        {
            Directory.CreateDirectory(_tusPath);
        }
    }

    public string GetTusPath() => _tusPath;
    public string GetVideosPath() => _videosPath;
    public string GetTusUrlPath() => TusUrlPath;

    public DefaultTusConfiguration GetTusConfiguration()
    {
        return new DefaultTusConfiguration
        {
            Store = new TusDiskStore(_tusPath),
            UrlPath = TusUrlPath,
            Events = new Events
            {
                OnFileCompleteAsync = async eventContext =>
                {
                    var file = await eventContext.GetFileAsync();
                    var metadata = await file.GetMetadataAsync(eventContext.CancellationToken);
                    
                    // Get the original filename and storage type from metadata
                    if (metadata.TryGetValue("filename", out var filenameMetadata) && 
                        metadata.TryGetValue("storageType", out var storageTypeMetadata))
                    {
                        var originalFilename = Encoding.UTF8.GetString(Convert.FromBase64String(filenameMetadata.GetString(Encoding.UTF8)));
                        var storageType = Encoding.UTF8.GetString(Convert.FromBase64String(storageTypeMetadata.GetString(Encoding.UTF8)));
                        var fileExtension = Path.GetExtension(originalFilename);
                        
                        // Generate a unique filename
                        var uniqueFilename = $"{Guid.NewGuid()}{fileExtension}";
                        var tusFilePath = Path.Combine(_tusPath, file.Id);

                        if (storageType == "s3")
                        {
                            await UploadToS3Async(tusFilePath, uniqueFilename);
                        }
                        else // local storage
                        {
                            var finalPath = Path.Combine(_videosPath, uniqueFilename);
                            if (File.Exists(tusFilePath))
                            {
                                File.Move(tusFilePath, finalPath);
                            }
                        }

                        // Clean up the TUS file
                        if (File.Exists(tusFilePath))
                        {
                            File.Delete(tusFilePath);
                        }
                    }
                }
            }
        };
    }

    public async Task UploadToS3Async(string filePath, string fileName)
    {
        var region = _configuration["AWS:Region"];
        var bucketName = _configuration["AWS:BucketName"];

        if (!string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(bucketName))
        {
            var s3Client = new AmazonS3Client(FallbackCredentialsFactory.GetCredentials(), 
                Amazon.RegionEndpoint.GetBySystemName(region));

            using var fileStream = File.OpenRead(filePath);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = fileName,
                BucketName = bucketName,
                ContentType = "video/mp4" // You might want to get this from metadata
            };

            var transferUtility = new TransferUtility(s3Client);
            await transferUtility.UploadAsync(uploadRequest);
        }
    }
} 