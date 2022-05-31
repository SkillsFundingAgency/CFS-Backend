using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ReleaseCandidateServiceTests
    {
        static IEnumerable<object[]> DataSetupNoChannelsButApproved =>
            new[] 
            {
                new object[]
                {
                    new PublishedProviderVersion { MajorVersion = 1, Status = PublishedProviderStatus.Approved },
                    new List<ReleaseChannel>()
                },
                new object[]
                {
                    new PublishedProviderVersion { MajorVersion = 1, Status = PublishedProviderStatus.Approved },
                    null
                }
            };

        static IEnumerable<object[]> DataSetupNoChannelsAndNotApproved =>
            new[]
            {
                new object[]
                {
                    new PublishedProviderVersion { MajorVersion = 1, Status = PublishedProviderStatus.Draft },
                    new List<ReleaseChannel>()
                },
                new object[]
                {
                    new PublishedProviderVersion { MajorVersion = 1, Status = PublishedProviderStatus.Draft },
                    null
                }
            };

        static IEnumerable<object[]> DataSetupMultiFundsTransferChannelsNotCandidate =>
            new[]
            {
                new object[]
                {
                    new PublishedProviderVersion { MajorVersion = 1 },
                    new ReleaseChannel[]
                    {
                        new ReleaseChannel { ChannelCode = "Statment", MajorVersion = 1 },
                        new ReleaseChannel { ChannelCode = "Payment", MajorVersion = 0 },
                        new ReleaseChannel { ChannelCode = "Contracting", MajorVersion = 1 }
                    }
                }
            };

        static IEnumerable<object[]> DataSetupMultiFundsTransferChannelsCandidate =>
            new[]
            {
                new object[]
                {
                    new PublishedProviderVersion { MajorVersion = 2 },
                    new ReleaseChannel[]
                    {
                        new ReleaseChannel { ChannelCode = "Statment", MajorVersion = 1 },
                        new ReleaseChannel { ChannelCode = "Payment", MajorVersion = 0 },
                        new ReleaseChannel { ChannelCode = "Contracting", MajorVersion = 1 }
                    }
                }
            };

        [TestMethod]
        [DynamicData(nameof(DataSetupNoChannelsButApproved))]
        public void IsReleaseCandidateIsTrueIfNoReleaseChannelsButApproved(PublishedProviderVersion publishedProviderVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            Assert.IsTrue(IsReleaseCandidateResult(publishedProviderVersion, releaseChannels));
        }

        [TestMethod]
        [DynamicData(nameof(DataSetupNoChannelsAndNotApproved))]
        public void IsReleaseCandidateIsFalseIfNoReleaseChannelsAndNotApproved(PublishedProviderVersion publishedProviderVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            Assert.IsFalse(IsReleaseCandidateResult(publishedProviderVersion, releaseChannels));
        }

        [TestMethod]
        [DynamicData(nameof(DataSetupMultiFundsTransferChannelsNotCandidate))]
        public void IsReleaseCandidateIsFalseIfUptodate(PublishedProviderVersion publishedProviderVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            Assert.IsFalse(IsReleaseCandidateResult(publishedProviderVersion, releaseChannels));
        }

        [TestMethod]
        [DynamicData(nameof(DataSetupMultiFundsTransferChannelsCandidate))]
        public void IsReleaseCandidateIsTrueIfPendingRelease(PublishedProviderVersion publishedProviderVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            Assert.IsTrue(IsReleaseCandidateResult(publishedProviderVersion, releaseChannels));
        }

        private bool IsReleaseCandidateResult(PublishedProviderVersion publishedProviderVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            ReleaseCandidateService releaseCandidateService = new ReleaseCandidateService();
            return releaseCandidateService.IsReleaseCandidate(publishedProviderVersion, releaseChannels);
        }
    }
}
