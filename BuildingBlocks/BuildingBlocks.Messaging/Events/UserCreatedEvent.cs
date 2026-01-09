namespace BuildingBlocks.Messaging.Events
{


    public class UserCreatedEvent : IntegrationEvent
    {
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;

        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;

        public string? Href { get; set; }
    }
}
