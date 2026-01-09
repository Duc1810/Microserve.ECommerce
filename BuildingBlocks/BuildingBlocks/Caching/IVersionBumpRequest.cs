

namespace BuildingBlocks.Caching
{
    public interface IVersionBumpRequest
    {
        IEnumerable<string> VersionScopesToBump();
    }
}
