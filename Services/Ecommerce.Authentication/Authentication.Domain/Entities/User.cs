using Microsoft.AspNetCore.Identity;
namespace Authentication.Domain.Entities
{
    public class User : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
       // public bool IsActive { get; set; } = true;
    }
}
