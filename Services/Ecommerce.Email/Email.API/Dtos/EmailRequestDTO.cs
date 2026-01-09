namespace Email.API.Dtos
{
    public class EmailRequestDTO
    {
        public string ToEmail { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string Body { get; set; } = default!;
    }
}
