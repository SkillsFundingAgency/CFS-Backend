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
        static IEnumerable<object[]> DataSetupNoChannels =>
            new[] 
            {
                new object[]
                {
                    1,
                    new List<ReleaseChannel>()
                },
                new object[]
                {
                    1,
                    null
                }
            };

        static IEnumerable<object[]> DataSetupMultiFundsTransferChannelsNotCandidate =>
            new[]
            {
                new object[]
                {
                    1,
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
                    2,
                    new ReleaseChannel[]
                    {
                        new ReleaseChannel { ChannelCode = "Statment", MajorVersion = 1 },
                        new ReleaseChannel { ChannelCode = "Payment", MajorVersion = 0 },
                        new ReleaseChannel { ChannelCode = "Contracting", MajorVersion = 1 }
                    }
                }
            };

        [TestMethod]
        [DynamicData(nameof(DataSetupNoChannels))]
        public void IsReleaseCandidateIsFalseIfNoReleaseChannels(int publishedProviderMajorVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            Assert.IsFalse(IsReleaseCandidateResult(publishedProviderMajorVersion, releaseChannels));
        }

        [TestMethod]
        [DynamicData(nameof(DataSetupMultiFundsTransferChannelsNotCandidate))]
        public void IsReleaseCandidateIsFalseIfUptodate(int publishedProviderMajorVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            Assert.IsFalse(IsReleaseCandidateResult(publishedProviderMajorVersion, releaseChannels));
        }

        [TestMethod]
        [DynamicData(nameof(DataSetupMultiFundsTransferChannelsCandidate))]
        public void IsReleaseCandidateIsTrueIfPendingRelease(int publishedProviderMajorVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            Assert.IsTrue(IsReleaseCandidateResult(publishedProviderMajorVersion, releaseChannels));
        }

        private bool IsReleaseCandidateResult(int publishedProviderMajorVersion, IEnumerable<ReleaseChannel> releaseChannels)
        {
            ReleaseCandidateService releaseCandidateService = new ReleaseCandidateService();
            return releaseCandidateService.IsReleaseCandidate(publishedProviderMajorVersion, releaseChannels);
        }
    }
}
