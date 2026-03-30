using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Production.API.Middleware;
public class ValidateFileAttribute : ActionFilterAttribute
{
    private readonly long _maxSize = 5 * 1024 * 1024; // 5MB

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var file = context.ActionArguments["file"] as IFormFile;

        if (file == null || file.Length == 0)
        {
            context.Result = new BadRequestObjectResult("File is empty");
            return;
        }

        if (file.Length > _maxSize)
        {
            context.Result = new BadRequestObjectResult("File size must be <= 5MB");
            return;
        }

        var allowedExtensions = new[] { ".jpg", ".png", ".pdf", ".doc", ".docx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(ext))
        {
            context.Result = new BadRequestObjectResult("Invalid file type");
            return;
        }
    }
}

