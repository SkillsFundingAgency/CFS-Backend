using CalculateFunding.Models;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies.External;
using CalculateFunding.Services.Core.Options;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Services
{
    [TestClass]
    public class AzureBearerTokenProviderTests
    {
        [TestMethod]
        public async Task GetToken_WhenTokenIsInCache_ReturnsTokenFromCache()
        {
            //Arrange
            string cachedToken = "this-is-a-token";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is("client-id"))
                .Returns(cachedToken);

            AzureBearerTokenOptions options = CreateOptions();

            AzureBearerTokenProvider tokenProvider = CreateTokenProvider(null, cacheProvider, options);

            //Act
            string token = await tokenProvider.GetToken();

            //Assert
            token
                .Should()
                .BeEquivalentTo(cachedToken);
        }

        [TestMethod]
        public void GetToken_WhenTokenIsNotInCacheAndTokenProxyReturnsNull_ThrowsException()
        {
            //Arrange
            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is("client-id"))
                .Returns((string)null);

            AzureBearerTokenOptions options = CreateOptions();

            IAzureBearerTokenProxy proxy = CreateTokenProxy();
            proxy
                .FetchToken(Arg.Any<AzureBearerTokenOptions>())
                .Returns((AzureBearerToken)null);

            AzureBearerTokenProvider tokenProvider = CreateTokenProvider(proxy, cacheProvider, options);

            //Act
            Func<Task> test = async () => await tokenProvider.GetToken();

            //Assert
            test
               .ShouldThrowExactly<Exception>()
               .Which
               .Message
               .Should()
               .Be("Failed to refersh access token for url: http://test-token-url/");
        }

        [TestMethod]
        public async Task GetToken_WhenTokenIsNotInCacheButReturnsFromProxy_SetsInCacheReturnsToken()
        {
            //Arrange
            AzureBearerToken azureBearerToken = new AzureBearerToken
            {
                AccessToken = "this-is-a-token",
                ExpiryLength = 3600
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<string>(Arg.Is("client-id"))
                .Returns((string)null);

            AzureBearerTokenOptions options = CreateOptions();

            IAzureBearerTokenProxy proxy = CreateTokenProxy();
            proxy
                .FetchToken(Arg.Any<AzureBearerTokenOptions>())
                .Returns(azureBearerToken);

            AzureBearerTokenProvider tokenProvider = CreateTokenProvider(proxy, cacheProvider, options);

            //Act
            string token = await tokenProvider.GetToken();

            //Assert
            token
                .Should()
                .BeEquivalentTo(azureBearerToken.AccessToken);

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<string>(Arg.Is("client-id"), Arg.Is("this-is-a-token"), Arg.Is(TimeSpan.FromSeconds(3240)), Arg.Is(false), null);
        }

        static AzureBearerTokenProvider CreateTokenProvider(IAzureBearerTokenProxy azureBearerTokenProxy = null, ICacheProvider cacheProvider = null, AzureBearerTokenOptions azureBearerTokenOptions = null)
        {
            return new AzureBearerTokenProvider(
                azureBearerTokenProxy ?? CreateTokenProxy(),
                cacheProvider ?? CreateCacheProvider(),
                azureBearerTokenOptions ?? CreateOptions());
        }

        static IAzureBearerTokenProxy CreateTokenProxy()
        {
            return Substitute.For<IAzureBearerTokenProxy>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static AzureBearerTokenOptions CreateOptions()
        {
            return new AzureBearerTokenOptions
            {
                Url = "http://test-token-url/",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                GrantType = "client_credentials",
                Scope = "http://test-sope"
            };
        }
    }
}
