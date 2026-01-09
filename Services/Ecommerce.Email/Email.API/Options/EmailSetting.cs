namespace Email.API.Options
{
    public class EmailSetting
    {
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string Host { get; set; } = default!;
        public string DisplayName { get; set; } = "Ecommerce";
        public int Port { get; set; } = 587;
    }
}
