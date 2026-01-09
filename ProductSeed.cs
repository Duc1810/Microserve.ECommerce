using System;

// Tests/Fixtures/ProductSeed.cs
public static class ProductSeed
{
    public static IEnumerable<Product> TenItems() => new[]
    {
        new Product { Id=1,  Name="iPhone 13",  Category=new(){"phone","apple"}, Price=900,  CreatedAt=DateTime.UtcNow.AddDays(-10)},
        new Product { Id=2,  Name="iPhone 14",  Category=new(){"phone","apple"}, Price=1100, CreatedAt=DateTime.UtcNow.AddDays(-8)},
        new Product { Id=3,  Name="Galaxy S22", Category=new(){"phone","samsung"}, Price=850,  CreatedAt=DateTime.UtcNow.AddDays(-7)},
        new Product { Id=4,  Name="MacBook Air",Category=new(){"laptop","apple"}, Price=1300, CreatedAt=DateTime.UtcNow.AddDays(-20)},
        new Product { Id=5,  Name="ThinkPad X1",Category=new(){"laptop","lenovo"}, Price=1500, CreatedAt=DateTime.UtcNow.AddDays(-3)},
        new Product { Id=6,  Name="iPad Pro",   Category=new(){"tablet","apple"}, Price=1200, CreatedAt=DateTime.UtcNow.AddDays(-12)},
        new Product { Id=7,  Name="Galaxy Tab", Category=new(){"tablet","samsung"},Price=700,  CreatedAt=DateTime.UtcNow.AddDays(-4)},
        new Product { Id=8,  Name="AirPods",    Category=new(){"accessory","apple"},Price=250, CreatedAt=DateTime.UtcNow.AddDays(-2)},
        new Product { Id=9,  Name="Magic Mouse",Category=new(){"accessory","apple"},Price=120, CreatedAt=DateTime.UtcNow.AddDays(-1)},
        new Product { Id=10, Name="Surface Go", Category=new(){"tablet","microsoft"},Price=600,CreatedAt=DateTime.UtcNow.AddDays(-6)},
    };
}
