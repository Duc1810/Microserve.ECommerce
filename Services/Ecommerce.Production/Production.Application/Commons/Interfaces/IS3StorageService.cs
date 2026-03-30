using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Production.Application.Commons.Interfaces;
public interface IS3StorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folderName);
    string GeneratePresignedUrl(string fileName, string folderName, double durationMin = 5);
    Task<bool> DeleteFileAsync(string fileKey);
    string GetFileUrl(string fileKey);

}

