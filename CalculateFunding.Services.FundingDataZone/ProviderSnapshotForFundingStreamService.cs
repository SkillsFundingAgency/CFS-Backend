using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Services.FundingDataZone.Interfaces;
using CalculateFunding.Services.FundingDataZone.SqlModels;

namespace CalculateFunding.Services.FundingDataZone
{
    public class ProviderSnapshotForFundingStreamService : IProviderSnapshotForFundingStreamService
    {
        private readonly IPublishingAreaRepository _publishingAreaRepository;

        public ProviderSnapshotForFundingStreamService(IPublishingAreaRepository publishingAreaRepository)
        {
            _publishingAreaRepository = publishingAreaRepository;
        }

        public async Task<IEnumerable<ProviderSnapshot>> GetProviderSnapshotsForFundingStream(string fundingStreamId)
        {
            IEnumerable<PublishingAreaProviderSnapshot> sqlResults = await _publishingAreaRepository.GetProviderSnapshots(fundingStreamId);

            List<ProviderSnapshot> results = new List<ProviderSnapshot>();
            foreach (var snapshot in sqlResults)
            {
                ProviderSnapshot providerSnapshot = MapProviderSnapshot(snapshot);
                if (providerSnapshot != null)
                {
                    results.Add(providerSnapshot);
                }
            }

            return results;
        }

        private ProviderSnapshot MapProviderSnapshot(PublishingAreaProviderSnapshot snapshot)
        {
            ProviderSnapshot providerSnapshot = new ProviderSnapshot()
            {
                ProviderSnapshotId = snapshot.ProviderSnapshotId,
                Created = snapshot.Created,
                Description = snapshot.Description,
                FundingStreamCode = snapshot.FundingStreamCode,
                FundingStreamName = snapshot.FundingStreamName,
                Name = snapshot.Name,
                TargetDate = snapshot.TargetDate,
                Version = snapshot.Version,
            };

            return providerSnapshot;
        }
    }
}
