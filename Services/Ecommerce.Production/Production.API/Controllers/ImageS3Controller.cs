using Microsoft.AspNetCore.Mvc;
using Production.API.Middleware;
using Production.Application.Commons.Interfaces;
using Production.Application.Dtos.S3;

namespace Production.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class UploadImageController : ControllerBase
{
    private readonly IS3StorageService _s3Service;
    private readonly IS3MultipartUploadService _s3MultipartUploadService;

    public UploadImageController(IS3StorageService s3Service, IS3MultipartUploadService s3MultipartUploadService)
    {
        _s3Service = s3Service;
        _s3MultipartUploadService = s3MultipartUploadService;
    }

    [HttpPost("upload-image")]
    [ValidateFile]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        var fileKey = await _s3Service.UploadFileAsync(file, "product-images");
        var url = _s3Service.GetFileUrl(fileKey);

        return Ok(new { Key = fileKey, PublicUrl = url });
    }

    [HttpGet("presigned-url")]
    public IActionResult GetPresignedUrl([FromQuery] string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return BadRequest("File name is required");

        var url = _s3Service.GeneratePresignedUrl(fileName, "product-videos", durationMin: 10);

        return Ok(new { UploadUrl = url });
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileKey)
    {
        var result = await _s3Service.DeleteFileAsync(fileKey);
        return result ? Ok("Deleted successfully") : BadRequest("Delete failed");
    }

    [HttpPost("multipart/init")]
    public async Task<IActionResult> InitMultipartUpload(
    [FromQuery] string fileName,
    [FromQuery] string contentType)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(contentType))
            return BadRequest("FileName and ContentType are required");

        var result = await _s3MultipartUploadService.InitUploadAsync(
            fileName,
            contentType,
            "product-videos");

        return Ok(result);
    }

    [HttpGet("multipart/presigned-part")]
    public IActionResult GetPresignedPartUrl(
    [FromQuery] string key,
    [FromQuery] string uploadId,
    [FromQuery] int partNumber)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(uploadId))
            return BadRequest("Key and UploadId are required");

        var url = _s3MultipartUploadService.GeneratePresignedPartUrl(
            key,
            uploadId,
            partNumber,
            durationMinutes: 10);

        return Ok(new { UploadUrl = url, PartNumber = partNumber });
    }

    [HttpPost("multipart/complete")]
    public async Task<IActionResult> CompleteMultipartUpload(
    [FromBody] CompleteMutipartUpload request)
    {
        if (request == null)
            return BadRequest("Invalid request");

        await _s3MultipartUploadService.CompleteUploadAsync(request);

        return Ok(new
        {
            Message = "Upload completed successfully",
            FileUrl = _s3MultipartUploadService.GetFileUrl(request.Key)
        });
    }

    [HttpDelete("multipart/abort")]
    public async Task<IActionResult> AbortMultipartUpload(
    [FromQuery] string key,
    [FromQuery] string uploadId)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(uploadId))
            return BadRequest("Key and UploadId are required");

        await _s3MultipartUploadService.AbortUploadAsync(key, uploadId);

        return Ok("Upload aborted successfully");
    }
}