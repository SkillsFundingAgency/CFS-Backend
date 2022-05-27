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

        public bool IsReleaseCandidate(int publishedProviderMajorVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            if (releaseChannels == null || !releaseChannels.Any())
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

            return releasedMajorVersions.Any(_ => _ < publishedProviderMajorVersion);
        }
    }
}
