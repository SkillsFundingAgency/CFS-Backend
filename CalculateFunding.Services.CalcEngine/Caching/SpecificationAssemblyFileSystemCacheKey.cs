using CalculateFunding.Services.Core.Caching.FileSystem;

namespace CalculateFunding.Services.CalcEngine.Caching
{
    public class SpecificationAssemblyFileSystemCacheKey : FileSystemCacheKey
    {
        public SpecificationAssemblyFileSystemCacheKey(string specificationId,
            string etag)
            : base($"SpecificationAssembly_{specificationId}_{etag.Replace("\"", "")}",
                "specification-assemblies")
        {
        }
    }
}