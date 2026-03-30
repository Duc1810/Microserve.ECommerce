
using System.ComponentModel.DataAnnotations;

namespace Production.Application.Dtos.S3;
public record InitMultipartUploadResponse
{
    public string UploadId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
}

