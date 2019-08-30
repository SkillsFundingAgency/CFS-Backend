using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V3.Interfaces
{
    public interface IFeedItemPreloader
    {
        Task BeginFeedItemPreLoading();
        void EnsureFoldersExists();
    }
}