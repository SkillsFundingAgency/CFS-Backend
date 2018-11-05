using AutoMapper;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Users.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

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
            ISpecificationRepository specificationRepository = null,
            IVersionRepository<FundingStreamPermissionVersion> fundingStreamPermissionVersionRepository = null,
            ICacheProvider cacheProvider = null,
            IMapper mapper = null,
            ILogger logger = null,
            IUsersResiliencePolicies policies = null)
        {
            return new FundingStreamPermissionService(
                userRepository ?? CreateUserRepository(),
                specificationRepository ?? CreateSpecificationRepository(),
                fundingStreamPermissionVersionRepository ?? CreateFundingStreamPermissionRepository(),
                cacheProvider ?? CreateCacheProvider(),
                mapper ?? CreateMappingConfiguration(),
                logger ?? CreateLogger(),
                policies ?? CreatePoliciesNoOp()
                );
        }

        public IUserRepository CreateUserRepository()
        {
            return Substitute.For<IUserRepository>();
        }

        public ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
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
    }
}
