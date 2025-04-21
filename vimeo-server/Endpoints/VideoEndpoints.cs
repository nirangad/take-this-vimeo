using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Runtime;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;
using System.Text;
using Amazon;
using vimeo_server.Services;
using Microsoft.AspNetCore.Mvc;

namespace vimeo_server.Endpoints;

public static class VideoEndpoints
{
    public static void MapVideoEndpoints(this IEndpointRouteBuilder app)
    {
        var videoGroup = app.MapGroup("/video");
        var videoUploadGroup = videoGroup.MapGroup("/upload");
        var videoUploadFormDataGroup = videoUploadGroup.MapGroup("/form-data");
        var videoUploadTUSGroup = videoUploadGroup.MapGroup("/tus");

        // [form-data] /video/upload/form-data/local
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

        // [form-data] /video/upload/form-data/s3
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

        // [tus] /video/upload/tus/local
        videoUploadTUSGroup.MapPost("/local", async ([FromForm] IFormFile? file, TusFileService tusService, ILogger<Program> logger) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest("No file was uploaded.");
                }

                // Create a temporary file in the TUS directory
                var tusPath = Path.Combine(tusService.GetVideosPath(), Guid.NewGuid().ToString());
                using (var stream = new FileStream(tusPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Process the file using TUS service
                var uniqueFilename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var finalPath = Path.Combine(tusService.GetVideosPath(), uniqueFilename);
                
                File.Move(tusPath, finalPath);

                return Results.Ok(new { message = "Video uploaded successfully", fileName = uniqueFilename });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading video via TUS");
                return Results.Problem("An error occurred while uploading the video.");
            }
        })
        .WithName("UploadVideoTusLocal")
        .WithOpenApi()
        .Accepts<IFormFile>("multipart/form-data")
        .DisableAntiforgery();

        // [tus] /video/upload/tus/s3
        videoUploadTUSGroup.MapPost("/s3", async ([FromForm] IFormFile? file, TusFileService tusService, ILogger<Program> logger) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest("No file was uploaded.");
                }

                // Create a temporary file in the TUS directory
                var tusPath = Path.Combine(tusService.GetVideosPath(), Guid.NewGuid().ToString());
                using (var stream = new FileStream(tusPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Process the file using TUS service
                var uniqueFilename = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                await tusService.UploadToS3Async(tusPath, uniqueFilename);

                // Clean up the temporary file
                if (File.Exists(tusPath))
                {
                    File.Delete(tusPath);
                }

                return Results.Ok(new { message = "Video uploaded to S3 successfully", fileName = uniqueFilename });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading video to S3 via TUS");
                return Results.Problem("An error occurred while uploading the video to S3.");
            }
        })
        .WithName("UploadVideoTusS3")
        .WithOpenApi()
        .Accepts<IFormFile>("multipart/form-data")
        .DisableAntiforgery();
    }
} 