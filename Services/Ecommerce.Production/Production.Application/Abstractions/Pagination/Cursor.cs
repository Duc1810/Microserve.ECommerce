

namespace Production.Application.Abstractions.Pagination
{

    public sealed record Cursor(DateTime Date, Guid LastId);

    public sealed record CursorRequest(string? Cursor, int PageSize = 20);

    public sealed record CursorPage<T>(IReadOnlyList<T> Data,  bool HasMore);
    public sealed record CursorResponse<T>(IReadOnlyList<T> Data, string? NextCursor, bool HasMore);

}
