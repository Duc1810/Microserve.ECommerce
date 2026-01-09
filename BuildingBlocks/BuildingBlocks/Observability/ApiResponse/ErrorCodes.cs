namespace BuildingBlocks.Observability.ApiResponse
{
    public static class ErrorCodes
    {
       

        // ==== Validation (validation.*) ====
        public const string ValidationFailed = "validation.failed";
        public const string InvalidInput = "validation.invalid_input";
        public const string MissingRequired = "validation.missing_required";

        // ==== Common / System (common.*) ====
        public const string BadRequest = "common.bad_request";
        public const string NotFound = "common.not_found";
        public const string Conflict = "common.conflict";
        public const string InternalError = "common.internal_error";
        public const string ServiceUnavailable = "common.service_unavailable";
        public const string OperationFailed = "common.operation_failed";

        // ==== User / Profile (user.*) ====
        public const string UserAlreadyExists = "user.already_exists";
        public const string PasswordWeak = "user.password_weak";

        // ==== Permission / Role (permission.*) ====
        public const string RoleNotFound = "permission.role_not_found";
        public const string AccessDenied = "permission.access_denied";

        // ==== Token / Session (token.*) ====
        public const string TokenInvalid = "token.invalid";
        public const string Unauthorized = "token.unauthorized";
        public const string TokenRevoked = "token.revoked";

        // ==== External / Integration (external.*) ====
        public const string ExternalServiceDown = "external.service_down";
        public const string ExternalTimeout = "external.timeout";
        public const string ExternalRejected = "external.rejected";
    }
}
