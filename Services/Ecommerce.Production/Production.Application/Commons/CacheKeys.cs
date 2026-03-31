namespace Production.Application.Commons;

public static class CacheKeys
{
    public static class Category
    {
        public const string Categories = "production:categories:v2";
        public const string CategoryStats = "production:category-stats:v2";
    }

    public static class Expiration
    {
        public static readonly TimeSpan Categories = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan CategoryStats = TimeSpan.FromMinutes(5);
    }
}
