using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing.Variations
{
    public class PublishedProviderSnapShots
    {
        public PublishedProviderSnapShots(PublishedProvider original)
        {
            Original = original.DeepCopy();
        }

        public PublishedProvider Refreshed { get; set; }
        
        public PublishedProvider Original { get; }

        public void AddRefreshedSnapshot(PublishedProvider refreshed)
        {
            Refreshed = refreshed.DeepCopy();
        }

        public PublishedProvider LatestSnapshot => Refreshed ?? Original;
    }
}