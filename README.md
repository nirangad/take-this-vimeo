# Take This Vimeo

A video uploading and streaming service built with .NET Core and React, supporting local storage, AWS S3 integration, and Vimeo platform integration.

## Features

- Video upload support with two methods:
  - Form-data upload
  - TUS protocol support (coming soon)
- Multiple storage options:
  - Local file system storage
  - AWS S3 integration
  - Vimeo platform integration (coming soon)
- RESTful API endpoints
- Swagger/OpenAPI documentation
- Secure HTTPS support

## Prerequisites

- .NET 7.0 or later
- Node.js and npm (for React frontend)
- AWS account (for S3 integration, optional)
- Vimeo API credentials (for Vimeo integration, optional)

## Setup

### Backend (.NET Core)

1. Navigate to the server directory:
   ```bash
   cd vimeo-server
   ```

2. Configure the application:
   - For local storage: No additional configuration needed
   - For S3 storage: Update `appsettings.json` with your AWS credentials:
     ```json
     {
       "AWS": {
         "Region": "your-aws-region",
         "BucketName": "your-s3-bucket-name"
       }
     }
     ```
   - For Vimeo integration: Update `appsettings.json` with your Vimeo credentials:
     ```json
     {
       "Vimeo": {
         "ClientId": "your-client-id",
         "ClientSecret": "your-client-secret",
         "AccessToken": "your-access-token"
       }
     }
     ```

3. Run the server:
   ```bash
   dotnet run
   ```

The server will start on `https://localhost:5001` with Swagger documentation available at `/swagger`.

## API Endpoints

### Video Upload

#### Local Storage
- **POST** `/video/upload/form-data/local`
  - Accepts multipart/form-data
  - Returns: `{ message: string, fileName: string }`

#### S3 Storage
- **POST** `/video/upload/form-data/s3`
  - Accepts multipart/form-data
  - Returns: `{ message: string, fileName: string }`

#### Vimeo Platform (Coming Soon)
- **POST** `/video/upload/form-data/vimeo`
  - Accepts multipart/form-data
  - Returns: `{ message: string, videoId: string, uri: string }`

## Development

### Project Structure
- `vimeo-server/` - .NET Core backend
  - `Endpoints/` - API endpoint definitions
  - `videos/` - Local storage directory
  - `appsettings.json` - Configuration file

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
