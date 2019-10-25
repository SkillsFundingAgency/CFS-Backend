using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task UpdatePublishedRefreshedDate_WhenNoSpecificationId_ThenBadRequestReturned()
        {
            // Arrange
            SpecificationsService specsService = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();

            // Act
            IActionResult result = await specsService.UpdatePublishedRefreshedDate(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Null or empty specification Id provided");
        }

        [TestMethod]
        public async Task UpdatePublishedRefreshedDate_WhenNoUpdateModel_ThenBadRequestReturned()
        {
            // Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            SpecificationsService specsService = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Query.Returns(queryStringValues);

            // Act
            IActionResult result = await specsService.UpdatePublishedRefreshedDate(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("Null refresh date model provided");
        }

        [TestMethod]
        public async Task UpdatePublishedRefreshedDate_WhenInvalidUpdateModel_ThenBadRequestReturned()
        {
            // Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            var wrongModel = new { PublishedResultsRefreshedAt = "wrong" }; // Wrong model
            string json = JsonConvert.SerializeObject(wrongModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            SpecificationsService specsService = CreateService();

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Query.Returns(queryStringValues);
            request.Body.Returns(stream);
            request.HttpContext.Returns(context);

            // Act
            IActionResult result = await specsService.UpdatePublishedRefreshedDate(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().Be("An invalid refresh date was provided");
        }

        [TestMethod]
        public async Task UpdatePublishedRefreshedDate_WhenUnknownSpecificationIdProvided_ThenNotFoundResultReturned()
        {
            // Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues("-1") }

            });

            UpdatePublishedRefreshedDateModel updateModel = new UpdatePublishedRefreshedDateModel
            {
                PublishedResultsRefreshedAt = DateTimeOffset.Now.AddHours(-1)
            };
            string json = JsonConvert.SerializeObject(updateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository();
            specsRepo.GetSpecificationById(Arg.Is("-1")).Returns((Specification)null);

            SpecificationsService specsService = CreateService(specificationsRepository: specsRepo);

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Query.Returns(queryStringValues);
            request.Body.Returns(stream);
            request.HttpContext.Returns(context);

            // Act
            IActionResult result = await specsService.UpdatePublishedRefreshedDate(request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>().Which.Value.Should().Be("Specification not found");
        }

        [TestMethod]
        public async Task UpdatePublishedRefreshedDate_WhenUpdateIsUnsuccessful_ThenInternalServerErrorReturned()
        {
            // Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            Specification specification = new Specification
            {
                Id = SpecificationId,
                Name = "spec1"
            };

            UpdatePublishedRefreshedDateModel updateModel = new UpdatePublishedRefreshedDateModel
            {
                PublishedResultsRefreshedAt = DateTimeOffset.Now.AddHours(-1)
            };
            string json = JsonConvert.SerializeObject(updateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository();
            specsRepo.GetSpecificationById(Arg.Is(SpecificationId)).Returns(specification);
            specsRepo.UpdateSpecification(Arg.Is(specification)).Returns(HttpStatusCode.InternalServerError);

            SpecificationsService specsService = CreateService(specificationsRepository: specsRepo);

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Query.Returns(queryStringValues);
            request.Body.Returns(stream);
            request.HttpContext.Returns(context);

            // Act
            IActionResult result = await specsService.UpdatePublishedRefreshedDate(request);

            // Assert
            result.Should().BeOfType<InternalServerErrorResult>().Which.Value.Should().Be($"Failed to set PublishedResultsRefreshedAt on specification for id: {SpecificationId} to value: {updateModel.PublishedResultsRefreshedAt.ToString()}");
        }

        [TestMethod]
        public async Task UpdatePublishedRefreshedDate_WhenUpdateIndexIsUnsuccessful_ThenInternalServerErrorReturned()
        {
            // Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            Specification specification = new Specification
            {
                Id = SpecificationId,
                Name = "spec1",
                Current = new SpecificationVersion
                {
                    Description = "test",
                    FundingPeriod = new Reference { Id = "fp1", Name = "funding period 1" },
                    FundingStreams = new List<FundingStream>
                    {
                        new FundingStream{ Id = "fs1", Name = "funding stream 1" }
                    },
                    PublishStatus = Models.Versioning.PublishStatus.Draft
                }
            };

            UpdatePublishedRefreshedDateModel updateModel = new UpdatePublishedRefreshedDateModel
            {
                PublishedResultsRefreshedAt = DateTimeOffset.Now.AddHours(-1)
            };
            string json = JsonConvert.SerializeObject(updateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository();
            specsRepo.GetSpecificationById(Arg.Is(SpecificationId)).Returns(specification);
            specsRepo.UpdateSpecification(Arg.Is(specification)).Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepo = CreateSearchRepository();
            searchRepo.Index(Arg.Any<IEnumerable<SpecificationIndex>>()).Returns(new List<IndexError> { new IndexError { ErrorMessage = "an error", Key = SpecificationId } });

            SpecificationsService specsService = CreateService(specificationsRepository: specsRepo, searchRepository: searchRepo);

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Query.Returns(queryStringValues);
            request.Body.Returns(stream);
            request.HttpContext.Returns(context);

            // Act
            IActionResult result = await specsService.UpdatePublishedRefreshedDate(request);

            // Assert
            result.Should().BeOfType<InternalServerErrorResult>().Which.Value.Should().Be($"Failed to index search for specification {SpecificationId} with the following errors: an error");
        }

        [TestMethod]
        public async Task UpdatePublishedRefreshedDate_WhenUpdateIsSuccessful_ThenOkResultReturned()
        {
            // Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            Specification specification = new Specification
            {
                Id = SpecificationId,
                Name = "spec1",
                Current = new SpecificationVersion
                {
                    Description = "test",
                    FundingPeriod = new Reference { Id = "fp1", Name = "funding period 1" },
                    FundingStreams = new List<Reference>
                    {
                        new Reference{ Id = "fs1", Name = "funding stream 1" }
                    },
                    PublishStatus = Models.Versioning.PublishStatus.Draft
                }
            };

            UpdatePublishedRefreshedDateModel updateModel = new UpdatePublishedRefreshedDateModel
            {
                PublishedResultsRefreshedAt = DateTimeOffset.Now.AddHours(-1)
            };
            string json = JsonConvert.SerializeObject(updateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            ISpecificationsRepository specsRepo = CreateSpecificationsRepository();
            specsRepo.GetSpecificationById(Arg.Is(SpecificationId)).Returns(specification);
            specsRepo.UpdateSpecification(Arg.Is(specification)).Returns(HttpStatusCode.OK);

            ISearchRepository<SpecificationIndex> searchRepo = CreateSearchRepository();
            searchRepo.Index(Arg.Any<IEnumerable<SpecificationIndex>>()).Returns(Enumerable.Empty<IndexError>());

            ICacheProvider cacheProvider = CreateCacheProvider();

            SpecificationsService specsService = CreateService(specificationsRepository: specsRepo, searchRepository: searchRepo, cacheProvider: cacheProvider);

            HttpRequest request = Substitute.For<HttpRequest>();
            request.Query.Returns(queryStringValues);
            request.Body.Returns(stream);
            request.HttpContext.Returns(context);

            // Act
            IActionResult result = await specsService.UpdatePublishedRefreshedDate(request);

            // Assert
            result.Should().BeOfType<OkResult>();

            await specsRepo.Received(1).UpdateSpecification(Arg.Is<Specification>(s => s.PublishedResultsRefreshedAt == updateModel.PublishedResultsRefreshedAt));
            await searchRepo.Received(1).Index(Arg.Is<IEnumerable<SpecificationIndex>>(l => l.Count() == 1 && l.First().PublishedResultsRefreshedAt == updateModel.PublishedResultsRefreshedAt));

            await cacheProvider
                .Received(1)
                .RemoveAsync<SpecificationSummary>($"{CacheKeys.SpecificationSummaryById}{specification.Id}");

            await cacheProvider
                .Received(1)
                .RemoveAsync<SpecificationCurrentVersion>($"{CacheKeys.SpecificationCurrentVersionById}{specification.Id}");
        }
    }
}
