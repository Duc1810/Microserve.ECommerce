using Production.Application.Dtos.S3;

namespace Production.Application.Commons.Interfaces;
public interface IS3MultipartUploadService
{
    // 1. Init upload
    Task<InitMultipartUploadResponse> InitUploadAsync(string fileName, string contentType, string folderName);

    // 2. Generate presigned URL each part
    string GeneratePresignedPartUrl(string key, string uploadId, int partNumber, double durationMinutes = 5);

    // 3. Complete upload
    Task CompleteUploadAsync(CompleteMutipartUpload completeMutipartUpload);

    // 4. Abort upload 
    Task AbortUploadAsync(string key, string uploadId);

    // 5. Get file URL 
    string GetFileUrl(string key);
}

