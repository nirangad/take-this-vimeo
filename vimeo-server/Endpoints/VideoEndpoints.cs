using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;

namespace vimeo_server.Endpoints;

public static class VideoEndpoints
{
    public static void MapVideoEndpoints(this IEndpointRouteBuilder app)
    {
        var videoGroup = app.MapGroup("/video");
        var videoUploadGroup = videoGroup.MapGroup("/upload");
        var videoUploadFormDataGroup = videoUploadGroup.MapGroup("/form-data");
        var videoUploadTUSGroup = videoUploadGroup.MapGroup("/tus");

        videoUploadFormDataGroup.MapPost("/local", async (IFormFile? file, IWebHostEnvironment environment, ILogger<Program> logger) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest("No file was uploaded.");
                }

                // Create videos directory if it doesn't exist
                var videosPath = Path.Combine(environment.ContentRootPath, "videos");
                if (!Directory.Exists(videosPath))
                {
                    Directory.CreateDirectory(videosPath);
                }

                // Generate a unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(videosPath, fileName);

                // Save the file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Results.Ok(new { message = "Video uploaded successfully", fileName });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading video");
                return Results.Problem("An error occurred while uploading the video.");
            }
        })
        .WithName("UploadVideo")
        .WithOpenApi()
        .DisableAntiforgery();

        videoUploadFormDataGroup.MapPost("/s3", async (IFormFile? file, IConfiguration configuration, ILogger<Program> logger) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest("No file was uploaded.");
                }

                var region = configuration["AWS:Region"];
                var bucketName = configuration["AWS:BucketName"];

                if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(bucketName))
                {
                    return Results.Problem("AWS configuration is missing.");
                }

                // Create S3 client using default profile
                var s3Client = new AmazonS3Client(FallbackCredentialsFactory.GetCredentials(), 
                    Amazon.RegionEndpoint.GetBySystemName(region));
                
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = memoryStream,
                    Key = fileName,
                    BucketName = bucketName,
                    ContentType = file.ContentType
                };

                var transferUtility = new TransferUtility(s3Client);
                await transferUtility.UploadAsync(uploadRequest);

                return Results.Ok(new { message = "Video uploaded to S3 successfully", fileName });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading video to S3");
                return Results.Problem("An error occurred while uploading the video to S3.");
            }
        })
        .WithName("UploadVideoToS3")
        .WithOpenApi()
        .DisableAntiforgery();
    }
} 