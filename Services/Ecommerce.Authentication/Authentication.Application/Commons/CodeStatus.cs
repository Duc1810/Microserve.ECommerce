namespace Authentication.Application.Commons
{
    public static class CodeStatus
    {
        // ==== Authentication - Common (auth.*) ====
        public const string UserNotFound = "auth.user_not_found";
        public const string InvalidCredentials = "auth.invalid_credentials";
        public const string UnableAccessToken = "auth.unable_access_token";
        public const string EmailNotConfirmed = "auth.email_not_confirmed";
        public const string TokenExpired = "auth.token_expired";
        public const string RefreshTokenInvalid = "auth.refresh_token_invalid";
        public const string Forbidden = "auth.forbidden";
        public const string Unauthorized = "auth.unauthorized";

        // ==== Register ====
        public const string EmailAlreadyExists = "auth.email_already_exists";
        public const string UsernameAlreadyExists = "auth.username_already_exists";
        public const string UserCreationFailed = "auth.user_creation_failed";

        // ==== Change Password ====
        public const string ChangePasswordFailed = "auth.change_password_failed";

        // ==== Revoke Token ====
        public const string UnableRevokeToken = "auth.unable_revoke_token";

        // ==== Email Confirmation / Verification ====
        public const string InvalidLink = "auth.invalid_link";
        public const string ConfirmEmailFailed = "auth.confirm_email_failed";
    }
}
