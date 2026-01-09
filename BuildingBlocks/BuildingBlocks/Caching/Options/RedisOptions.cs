
namespace BuildingBlocks.Caching.Options
{
    public class RedisOptions
    {
        public static readonly string OptionName = "Redis";
        public string Host { get; set; }
        public string Port { get; set; }
        public string Password { get; set; } = string.Empty;
        public bool IsSSL { get; set; } = false;
        public string Prefix { get; set; } = "app:";

        public int Database { get; set; } = 0;
    }
}
