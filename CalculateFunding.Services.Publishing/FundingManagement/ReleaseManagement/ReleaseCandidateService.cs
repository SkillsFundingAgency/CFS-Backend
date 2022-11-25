using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ReleaseCandidateService : IReleaseCandidateService
    {
        private string[] fundsTransferChannels = { "Contracting", "Payment" };

        public bool IsReleaseCandidate(PublishedProviderVersion publishedProviderVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            if (publishedProviderVersion.Status == PublishedProviderStatus.Approved)
            {
                return true;
            }

            if (releaseChannels == null || !releaseChannels.Any() || publishedProviderVersion.Status == PublishedProviderStatus.Updated)
            {
                return false;
            }

            List<int> releasedMajorVersions = releaseChannels.Where(_ => !fundsTransferChannels.Contains(_.ChannelCode))
                .Select(_ => _.MajorVersion)
                .ToList();

            int? fundsTransferMajorVersion = releaseChannels.Where(_ => fundsTransferChannels.Contains(_.ChannelCode))
                                                ?.Max(_ => _.MajorVersion);

            if (fundsTransferMajorVersion != null)
            {
                releasedMajorVersions.Add((int)fundsTransferMajorVersion);
            }

            return releasedMajorVersions.Any(_ => _ < publishedProviderVersion.MajorVersion);
        }
    }
}
