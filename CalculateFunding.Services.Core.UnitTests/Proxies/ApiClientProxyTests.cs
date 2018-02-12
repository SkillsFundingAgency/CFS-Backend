using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Core.Options;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Proxies
{
    [TestClass]
    public class ApiClientProxyTests
    {
        [TestMethod]
        public void GetAsync_GivenEmptyUrl_ThrowsException()
        {
            //Arrange
            ApiClientProxy proxy = CreateApiClientProxy();

            //Act
            Func<Task> test = async () => await proxy.GetAsync<TestClass>("");

            //Assert
            test
                .ShouldThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public void GetAsync_GivenResponseIsNull_ThrowsHttpRequestException()
        {
            //Arrange
            const string url = "any-url";

            IHttpClient httpClient = CreateHttpClient();
            httpClient
                .GetAsync(Arg.Is(url))
                .Returns((HttpResponseMessage)null);

            ApiClientProxy proxy = CreateApiClientProxy(httpClient: httpClient);

            //Act
            Func<Task> test = async () => await proxy.GetAsync<TestClass>(url);

            //Assert
            test
                .ShouldThrowExactly<HttpRequestException>();
        }

        [TestMethod]
        async public Task GetAsync_GivenHttpClientReturnsNoSuccessCode_ReturnsNull()
        {
            //Arrange
            const string url = "any-url";

            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);

            IHttpClient httpClient = CreateHttpClient();
            httpClient
                .GetAsync(Arg.Is(url))
                .Returns(responseMessage);

            ApiClientProxy proxy = CreateApiClientProxy(httpClient: httpClient);

            //Act
            TestClass testClass = await proxy.GetAsync<TestClass>(url);

            //Assert
            testClass
                .Should()
                .BeNull();
        }

        [TestMethod]
        async public Task GetAsync_GivenHttpClientReturnsSuccesfulResponse_ReturnsContent()
        {
            //Arrange
            const string url = "any-url";

            TestClass model = new TestClass
            {
                Id = "test-id"
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage
                .Content = new StreamContent(stream);

            IHttpClient httpClient = CreateHttpClient();
            httpClient
                .GetAsync(Arg.Is(url))
                .Returns(responseMessage);

            ApiClientProxy proxy = CreateApiClientProxy(httpClient: httpClient);

            //Act
            TestClass testClass = await proxy.GetAsync<TestClass>(url);

            //Assert
            testClass
                .Id
                .Should()
                .Be("test-id");
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ApiOptions CreateApiOptions()
        {
            return new ApiOptions
            {
                ApiEndpoint = "http://wherever/",
                ApiKey = "1234567"
            };
        }

        static IHttpClient CreateHttpClient()
        {
            return Substitute.For<IHttpClient>();
        }

        static ICorrelationIdProvider CreateCorrelationIdProvider(string correlattionId = "")
        {
            string id = string.IsNullOrWhiteSpace(correlattionId) ? Guid.NewGuid().ToString() : correlattionId;

            ICorrelationIdProvider correlationIdProvider = Substitute.For<ICorrelationIdProvider>();
            correlationIdProvider
                .GetCorrelationId()
                .Returns(id);

            return correlationIdProvider;
        }

        static ApiClientProxy CreateApiClientProxy(ApiOptions options = null, IHttpClient httpClient = null, 
            ILogger logger = null, ICorrelationIdProvider correlationIdProvider = null)
        {
            return new ApiClientProxy(options ?? CreateApiOptions(), httpClient ?? CreateHttpClient(),
                logger ?? CreateLogger(), correlationIdProvider ?? CreateCorrelationIdProvider());
        }

        class TestClass
        {
            public string Id { get; set; }
        }
    }
}
