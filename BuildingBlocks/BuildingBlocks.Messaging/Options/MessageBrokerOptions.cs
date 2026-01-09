


namespace BuildingBlocks.Messaging.Options
{
    public class MessageBrokerOptions
    {
        public string Host { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;
        public RoutingKeysOptions RoutingKeys { get; set; } = new();
    }


    public class RoutingKeysOptions
    {
        public string UserCreated { get; set; } = default!;
    }
}


