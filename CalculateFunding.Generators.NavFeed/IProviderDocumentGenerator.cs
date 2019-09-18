using CalculateFunding.Generators.NavFeed.Options;
using System.Threading.Tasks;

namespace CalculateFunding.Generators.NavFeed
{
    public interface IProviderDocumentGenerator
    {
        Task<int> Generate(FeedOptions options);
    }
}
