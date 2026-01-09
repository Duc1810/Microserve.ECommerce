namespace Authentication.Application.Dtos.User
{
    public sealed class UserProfileDto
    {
        public string? UserId { get; init; }
        public string? UserName { get; init; }
        public string? Email { get; init; }
        public string[] Roles { get; init; } = [];
    }
}
