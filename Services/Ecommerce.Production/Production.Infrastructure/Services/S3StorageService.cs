

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Hangfire.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Production.Application.Commons.Interfaces;
using Production.Application.Commons.Options;
using Production.Application.Dtos.S3;

namespace Production.Infrastructure.Services;
public class S3StorageService : IS3StorageService, IS3MultipartUploadService
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _options;
    private readonly ILogger<S3StorageService> _logger;

    public S3StorageService(IAmazonS3 s3Client, IOptions<S3Options> options, ILogger<S3StorageService> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> DeleteFileAsync(string fileKey)
    {
        _logger.LogInformation("Request to delete file from S3. Key: {Key}", fileKey);

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = fileKey
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest);

            _logger.LogInformation("File deleted successfully or does not exist. Key: {Key}, Status: {Status}",
                fileKey, response.HttpStatusCode);

            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error occurred while deleting file. Key: {Key}", fileKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting file from S3. Key: {Key}", fileKey);
            return false;
        }
    }

    public string GeneratePresignedUrl(string fileName, string folderName, double durationMin = 5)
    {
        var extension = Path.GetExtension(fileName);
        var safeFileKey = $"{folderName}/{Guid.NewGuid()}{extension}";

        _logger.LogInformation("Generating URL. Key: {Key}", safeFileKey);

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = safeFileKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(durationMin),
                ContentType = "application/pdf"
            };

            return _s3Client.GetPreSignedURL(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate URL for: {FileName}", fileName);
            throw;
        }
    }

    public string GetFileUrl(string fileKey)
    {
        return $"https://{_options.BucketName}.s3.amazonaws.com/{fileKey}";
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName)
    {
        var extension = Path.GetExtension(file.FileName);
        var fileKey = $"{folderName}/{Guid.NewGuid()}{extension}";
        _logger.LogInformation("Initiating file upload to S3. Bucket: {Bucket}, Key: {Key}", _options.BucketName, fileKey);

        try
        {
            using var stream = file.OpenReadStream();
            var uploadRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = fileKey,
                InputStream = stream,
                ContentType = file.ContentType,
            };



            await _s3Client.PutObjectAsync(uploadRequest);

            _logger.LogInformation("Successfully uploaded file to S3. Key: {Key}", fileKey);
            return fileKey;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 error occurred while uploading file. Key: {Key}. Error: {Message}", fileKey, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during file upload. Key: {Key}", fileKey);
            throw;
        }
    }

    public async Task<InitMultipartUploadResponse> InitUploadAsync(string fileName, string contentType, string folderName)
    {
        _logger.LogInformation("Initializing multipart upload. FileName: {FileName}, Folder: {Folder}",
        fileName, folderName);

        try
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var key = $"{folderName}/{uniqueFileName}";

            var request = new InitiateMultipartUploadRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                ContentType = contentType,
                Metadata =
            {
                ["file-name"] = fileName
            }
            };

            var response = await _s3Client.InitiateMultipartUploadAsync(request);

            _logger.LogInformation("Multipart upload initialized successfully. Key: {Key}, UploadId: {UploadId}",
            key, response.UploadId);

            return new InitMultipartUploadResponse
            {
                UploadId = response.UploadId,
                Key = key,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while initializing multipart upload. FileName: {FileName}", fileName);
            throw;
        }
    }

    public string GeneratePresignedPartUrl(string key, string uploadId, int partNumber, double durationMinutes = 5)
    {
        _logger.LogInformation(
        "Generating presigned URL for multipart upload. Key: {Key}, UploadId: {UploadId}, PartNumber: {PartNumber}",
        key, uploadId, partNumber);

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(durationMinutes),
                UploadId = uploadId,
                PartNumber = partNumber,
            };

            var url = _s3Client.GetPreSignedURL(request);

            _logger.LogInformation(
           "Presigned URL generated successfully. PartNumber: {PartNumber}", partNumber);

            return url;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while generating presigned part URL. Key: {Key}, UploadId: {UploadId}",
                key, uploadId);
            throw;
        }
    }

    public async Task AbortUploadAsync(string key, string uploadId)
    {
        _logger.LogWarning(
                "Aborting multipart upload. Key: {Key}, UploadId: {UploadId}",
                key, uploadId);

        try
        {
            var request = new AbortMultipartUploadRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                UploadId = uploadId
            };

            await _s3Client.AbortMultipartUploadAsync(request);

            _logger.LogInformation("Multipart upload aborted successfully. Key: {Key}, UploadId: {UploadId}",
                key, uploadId);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while aborting multipart upload. Key: {Key}, UploadId: {UploadId}",
                key, uploadId);
            throw;
        }
    }

    public async Task CompleteUploadAsync(CompleteMutipartUpload completeMutipartUpload)
    {
        _logger.LogInformation(
        "Completing multipart upload. Key: {Key}, UploadId: {UploadId}",
        completeMutipartUpload.Key,
        completeMutipartUpload.UploadId);

        try
        {
            var partETags = completeMutipartUpload.Parts
                .OrderBy(p => p.PartNumber)
                .Select(p => new PartETag(p.PartNumber, p.ETag))
                .ToList();

            var request = new CompleteMultipartUploadRequest
            {
                BucketName = _options.BucketName,
                Key = completeMutipartUpload.Key,
                UploadId = completeMutipartUpload.UploadId,
                PartETags = partETags
            };

            var response = await _s3Client.CompleteMultipartUploadAsync(request);

            _logger.LogInformation(
                "Multipart upload completed successfully. Location: {Location}",
                response.Location);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while completing multipart upload. Key: {Key}",
                completeMutipartUpload.Key);
            throw;
        }
    }
}

