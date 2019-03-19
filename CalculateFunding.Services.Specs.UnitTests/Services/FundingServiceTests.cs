using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    [TestClass]
    public partial class FundingServiceTests
    {
        private const string FundingStreamId = "YAPGG";

        protected IFundingService CreateService(ISpecificationsRepository specificationsRepository = null, ICacheProvider cacheProvider = null, ILogger logger = null)
        {
            return new FundingService(specificationsRepository ?? CreateSpecificationsRepository(),
                cacheProvider ?? CreateCacheProvider(),
                logger ?? CreateLogger());
        }

        protected ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        protected ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        protected ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
