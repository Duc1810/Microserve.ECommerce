using System.Text.Json;


namespace Production.Application.Abstractions.Pagination
{
    public static class CursorCodec
    {
        public static string Encode(Cursor p)
            => Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(
                   JsonSerializer.SerializeToUtf8Bytes(p));

        public static Cursor? Decode(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            try
            {
                var bytes = Microsoft.IdentityModel.Tokens.Base64UrlEncoder.DecodeBytes(s);
                return JsonSerializer.Deserialize<Cursor>(bytes);
            }
            catch { return null; }
        }
    }
}
