using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Services.Core.Interfaces.Helpers;

namespace CalculateFunding.Services.Users
{
    [TestClass]
    public partial class FundingStreamPermissionServiceTests
    {
        public const string SpecificationId = "specId1";
        public const string UserId = "user1";
        public const string FundingStreamId = "fs1";

        public FundingStreamPermissionService CreateService(
            IUserRepository userRepository = null,
            ISpecificationsApiClient specificationsApiClient = null,
            IVersionRepository<FundingStreamPermissionVersion> fundingStreamPermissionVersionRepository = null,
            ICacheProvider cacheProvider = null,
            IMapper mapper = null,
            ILogger logger = null,
            IUsersResiliencePolicies policies = null,
            IUsersCsvGenerator usersCsvGenerator = null,
            IEnvironmentProvider environmentProvider = null)
        {
            return new FundingStreamPermissionService(
                userRepository ?? CreateUserRepository(),
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                fundingStreamPermissionVersionRepository ?? CreateFundingStreamPermissionRepository(),
                cacheProvider ?? CreateCacheProvider(),
                mapper ?? CreateMappingConfiguration(),
                logger ?? CreateLogger(),
                policies ?? CreatePoliciesNoOp(),
                usersCsvGenerator ?? CreateUsersCsvGenerator(),
                environmentProvider ?? CreateEnvironmentProvider());
        }

        public IEnvironmentProvider CreateEnvironmentProvider()
        {
            return Substitute.For<IEnvironmentProvider>();
        }

        public IUserRepository CreateUserRepository()
        {
            return Substitute.For<IUserRepository>();
        }

        static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        public IVersionRepository<FundingStreamPermissionVersion> CreateFundingStreamPermissionRepository()
        {
            return Substitute.For<IVersionRepository<FundingStreamPermissionVersion>>();
        }

        public ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        public IMapper CreateMappingConfiguration()
        {
            MapperConfiguration mapperConfiguration = new MapperConfiguration(c => c.AddProfile<UsersMappingProfile>());

            return mapperConfiguration.CreateMapper();
        }

        public ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public IUsersResiliencePolicies CreatePoliciesNoOp()
        {
            return UsersResilienceTestHelper.GenerateTestPolicies();
        }

        public IUsersCsvGenerator CreateUsersCsvGenerator()
        {
            return Substitute.For<IUsersCsvGenerator>();
        }
    }
}
