using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Interfaces
{
    public interface IFeedItemPreloader
    {
        Task BeginFeedItemPreLoading();
        void EnsureFoldersExists();
    }
}