using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishingFeatureFlag
    {
        Task<bool> IsVariationsEnabled();
    }
}
