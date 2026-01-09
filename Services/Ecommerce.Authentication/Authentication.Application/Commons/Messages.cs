namespace Authentication.Application.Commons
{
    public static class Messages
    {
        // Auth - Login
        public const string UserNotFound = "User not found.";
        public const string InvalidCredentials = "Invalid username or password.";
        public const string UnableAccessToken = "Unable to obtain access token.";

        // Auth - Change Password
        public const string ChangePasswordFailed = "Failed to change password.";

        // Auth - Revoke Token
        public const string UnableRevokeToken = "Failed to revoke token.";
        // Confirm Email
        public const string InvalidConfirmationLink = "Invalid confirmation link.";
        public const string InvalidOrExpiredLink = "Confirmation link is invalid or expired.";
        public const string ConfirmEmailFailed = "Failed to confirm email.";

    }
}
