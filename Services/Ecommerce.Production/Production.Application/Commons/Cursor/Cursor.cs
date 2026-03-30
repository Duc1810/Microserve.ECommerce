using Microsoft.AspNetCore.Authentication;
using System.Text;
using System.Text.Json;


namespace Production.Application.Commons.Cursor;
public sealed record Cursor(DateTime CreatedAt, Guid LastId)
{
    public static string Encode(DateTime createdAt, Guid lastId)
    {
        var json = JsonSerializer.Serialize(new Cursor(createdAt, lastId));
        return Base64UrlTextEncoder.Encode(Encoding.UTF8.GetBytes(json));
    }

    public static Cursor? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlTextEncoder.Decode(cursor));
            return JsonSerializer.Deserialize<Cursor>(json);
        }
        catch
        {
            return null;
        }
    }
}

