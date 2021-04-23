using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Specifications;
using CalculateFunding.Models.Specifications.ViewModels;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using CalculateFunding.Services.Specifications;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specifications.UnitTests.Validators;

namespace CalculateFunding.Services.Specifictions.UnitTests
{
    [TestClass]
    public class FundingStructureServiceTests
    {
        //this is lifted from the FE project pretty much as is

        private const string FundingStreamId = "DSG";
        private const string FundingPeriodId = "AY-2021";
        private const string TemplateVersion = "1.0";
        private const string SpecificationId = "680898bd-9ddc-4d11-9913-2a2aa34f213c";
        private const string aValidCalculationId1 = "aValidCalculationId-1";
        private const string aValidCalculationId2 = "aValidCalculationId-2";
        private const string aValidCalculationId3 = "aValidCalculationId-3";

        private const PublishStatus CalculationExpectedPublishStatus = PublishStatus.Approved;

        private ISpecificationsService _specificationsService;
        private ICalculationsApiClient _calculationsApiClient;
        private IGraphApiClient _graphApiClient;
        private ICacheProvider _cacheProvider;
        private Common.ApiClient.Policies.IPoliciesApiClient _policiesApiClient;
        private IValidator<UpdateFundingStructureLastModifiedRequest> _validator;

        private FundingStructureService _service;

        [TestInitialize]
        public void SetUp()
        {
            _specificationsService = Substitute.For<ISpecificationsService>();
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();
            _graphApiClient = Substitute.For<IGraphApiClient>();
            _cacheProvider = Substitute.For<ICacheProvider>();
            _policiesApiClient = Substitute.For<Common.ApiClient.Policies.IPoliciesApiClient>();
            _validator = Substitute.For<IValidator<UpdateFundingStructureLastModifiedRequest>>();

            _service = new FundingStructureService(
                _cacheProvider,
                _specificationsService,
                _calculationsApiClient,
                _graphApiClient,
                _policiesApiClient,
                _validator,
                new SpecificationsResiliencePolicies
                {
                    CacheProvider = Polly.Policy.NoOpAsync(),
                    CalcsApiClient = Polly.Policy.NoOpAsync(),
                    PoliciesApiClient = Polly.Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void UpdateFundingStructureLastModifiedGuardsAgainstMissingRequest()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheFundingStructureLastModifiedIsUpdated(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .BeEquivalentTo("request");
        }

        [TestMethod]
        public async Task UpdateFundingStructureLastModifiedGuardsAgainstInValidRequests()
        {
            UpdateFundingStructureLastModifiedRequest request = NewUpdateFundingStructureLastModifiedRequest();
            ValidationFailure failureOne = NewValidationFailure();
            ValidationFailure failureTwo = NewValidationFailure();
            ValidationResult validationResult = NewValidationResult(_ => _.WithValidationFailures(failureOne, failureTwo));

            GivenTheValidationResult(request, validationResult);

            BadRequestObjectResult actionResult = await WhenTheFundingStructureLastModifiedIsUpdated(request) as BadRequestObjectResult;

            SerializableError serializableError = actionResult?.Value as SerializableError;

            serializableError
                .Should()
                .NotBeNull();

            serializableError[failureOne.PropertyName]
                .Should()
                .BeEquivalentTo(new[] { failureOne.ErrorMessage });

            serializableError[failureTwo.PropertyName]
                .Should()
                .BeEquivalentTo(new[] { failureTwo.ErrorMessage });

            await AndTheCacheWasNotUpdated();
        }

        [TestMethod]
        public async Task UpdateFundingStructureLastModifiedSetsTheDateTimeOffsetValueInTheCache()
        {
            UpdateFundingStructureLastModifiedRequest request = NewUpdateFundingStructureLastModifiedRequest();
            GivenTheValidationResult(request, NewValidationResult());

            OkResult actionResult = await WhenTheFundingStructureLastModifiedIsUpdated(request) as OkResult;

            actionResult
                .Should()
                .NotBeNull();

            await AndTheLastUpdatedCacheWasUpdated($"{CacheKeys.FundingLineStructureTimestamp}{request.SpecificationId}:{request.FundingStreamId}:{request.FundingPeriodId}",
                request.LastModified);
        }

        [TestMethod]
        public async Task GetFundingStructureTimeStampLoadsTheCachedValueForTheFundingStructure()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            DateTimeOffset expectedLastModified = NewRandomDateTime();

            GivenTheLastModifiedDateForTheFundingStructure(specificationId, fundingStreamId, fundingPeriodId, expectedLastModified);

            DateTimeOffset actualLastModified = await WhenTheLastModifiedDateIsQueried(specificationId, fundingStreamId, fundingPeriodId);

            actualLastModified
                .Should()
                .Be(expectedLastModified);
        }

        [TestMethod]
        public void GetFundingStructureTimeStampGuardsAgainstMissingFundingPeriodId()
        {
            Func<Task<DateTimeOffset>> invocation = () => WhenTheLastModifiedDateIsQueried(NewRandomString(),
                NewRandomString(),
                null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingPeriodId");
        }

        [TestMethod]
        public void GetFundingStructureTimeStampGuardsAgainstMissingFundingStreamId()
        {
            Func<Task<DateTimeOffset>> invocation = () => WhenTheLastModifiedDateIsQueried(NewRandomString(),
                null,
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [TestMethod]
        public void GetFundingStructureTimeStampGuardsAgainstMissingSpecificationId()
        {
            Func<Task<DateTimeOffset>> invocation = () => WhenTheLastModifiedDateIsQueried(null,
                NewRandomString(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }


        private void GivenTheLastModifiedDateForTheFundingStructure(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            DateTimeOffset lastModified)
            => _cacheProvider.GetAsync<DateTimeOffset>($"{CacheKeys.FundingLineStructureTimestamp}{specificationId}:{fundingStreamId}:{fundingPeriodId}")
                .Returns(lastModified);

        private async Task<DateTimeOffset> WhenTheLastModifiedDateIsQueried(string specificationId,
            string fundingStreamId,
            string fundingPeriodId)
            => await _service.GetFundingStructureTimeStamp(fundingStreamId, fundingPeriodId, specificationId);

        private async Task AndTheLastUpdatedCacheWasUpdated(string key, DateTimeOffset value)
            => await _cacheProvider.Received(1)
                .SetAsync(Arg.Is(key), Arg.Is(value));

        private async Task AndTheCacheWasNotUpdated()
            => await _cacheProvider.Received(0)
                .SetAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>());

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder validationResultBuilder = new ValidationResultBuilder();

            setUp?.Invoke(validationResultBuilder);

            return validationResultBuilder
                .Build();
        }

        private ValidationFailure NewValidationFailure(Action<ValidationFailureBuilder> setUp = null)
        {
            ValidationFailureBuilder validationFailureBuilder = new ValidationFailureBuilder();

            setUp?.Invoke(validationFailureBuilder);

            return validationFailureBuilder.Build();
        }

        private void GivenTheValidationResult(UpdateFundingStructureLastModifiedRequest request,
            ValidationResult validationResult)
        {
            _validator.Validate(request)
                .Returns(validationResult);
        }

        private UpdateFundingStructureLastModifiedRequest NewUpdateFundingStructureLastModifiedRequest()
            => new UpdateFundingStructureLastModifiedRequestBuilder()
                .WithLastModified(NewRandomDateTime())
                .WithSpecificationId(NewRandomString())
                .WithFundingPeriodId(NewRandomString())
                .WithFundingStreamId(NewRandomString())
                .Build();

        private DateTimeOffset NewRandomDateTime() => new RandomDateTime();

        private string NewRandomString() => new RandomString();

        private async Task<IActionResult> WhenTheFundingStructureLastModifiedIsUpdated(UpdateFundingStructureLastModifiedRequest request)
            => await _service.UpdateFundingStructureLastModified(request);

        [TestMethod]
        public async Task GetFundingStructures_GivenTemplateMappingsIncompleteReturnsException()
        {
            ScenarioSetup(FundingStreamId, true);

            IActionResult apiResponseResult = await _service.GetFundingStructure(FundingStreamId, FundingPeriodId, SpecificationId);

            apiResponseResult.Should().BeOfType<InternalServerErrorResult>();
            InternalServerErrorResult typedResult = apiResponseResult as InternalServerErrorResult;
            typedResult.Value.ToString()
                .Should()
                .Be($"Template mappings missing for specification '{SpecificationId}' and funding stream '{FundingStreamId}'");
        }

        [TestMethod]
        public async Task GetFundingStructures_ReturnsFlatStructureWithCorrectLevelsAndInCorrectOrder()
        {
            ScenarioSetup(FundingStreamId);

            IActionResult apiResponseResult = await _service.GetFundingStructure(FundingStreamId, FundingPeriodId, SpecificationId);

            List<FundingStructureItem> expectedFundingStructureItems = GetValidMappedFundingStructureItems();
            apiResponseResult.Should().BeOfType<OkObjectResult>();
            OkObjectResult typedResult = apiResponseResult as OkObjectResult;
            FundingStructure fundingStructureItems = typedResult?.Value as FundingStructure;
            fundingStructureItems?.Items.Count().Should().Be(4);
            fundingStructureItems?.Items.Should().BeEquivalentTo(expectedFundingStructureItems);
        }

        [TestMethod]
        public async Task GetFundingStructures_ThrowsInternalErrorIfTemplateIdNotSet()
        {
            ScenarioSetup(FundingStreamId.ToLowerInvariant());

            IActionResult apiResponseResult = await _service.GetFundingStructure(FundingStreamId, FundingPeriodId, SpecificationId);

            apiResponseResult.Should().BeOfType<InternalServerErrorResult>();
        }

        private void ScenarioSetup(string fundingStreamId, bool missingTemplateMapping = false)
        {
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = SpecificationId,
                TemplateIds = new Dictionary<string, string>
                {
                    [fundingStreamId] = TemplateVersion
                }
            };

            TemplateMetadataContents templateMetadataContents = new TemplateMetadataContents
            {
                RootFundingLines = new List<FundingLine>
                {
                    new FundingLine
                    {
                        TemplateLineId = 123,
                        Name = "FundingLine-1"
                    },
                    new FundingLine
                    {
                        TemplateLineId = 234,
                        Name = "FundingLine-2-withFundingLines",
                        FundingLines = new List<FundingLine>
                        {
                            new FundingLine
                            {
                                TemplateLineId = 345,
                                Name = "FundingLine-2-fl-1"
                            },
                            new FundingLine
                            {
                                TemplateLineId = 456,
                                Name = "FundingLine-2-fl-2",
                                FundingLines = new List<FundingLine>
                                {
                                    new FundingLine
                                    {
                                        TemplateLineId = 890,
                                        Name = "FundingLine-2-fl-2-fl-1"
                                    }
                                }
                            }
                        }
                    },
                    new FundingLine
                    {
                        TemplateLineId = 567,
                        Name = "FundingLine-3-withCalculationsAndFundingLines",
                        FundingLines = new List<FundingLine>
                        {
                            new FundingLine
                            {
                                TemplateLineId = 678,
                                Name = "FundingLine-3-fl-1"
                            }
                        },
                        Calculations = new List<Calculation>
                        {
                            new Calculation
                            {
                                Name = "FundingLine-3-calc-1",
                                TemplateCalculationId = 7,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash
                            },
                            new Calculation
                            {
                                Name = "FundingLine-3-calc-2",
                                TemplateCalculationId = 11,
                                Calculations = new List<Calculation>
                                {
                                    new Calculation
                                    {
                                        Name = "FundingLine-3-calc-2-calc-1",
                                        TemplateCalculationId = 8,
                                        Type = Common.TemplateMetadata.Enums.CalculationType.Cash
                                    }
                                }
                            },
                            new Calculation
                            {
                                Name = "FundingLine-3-calc-3",
                                TemplateCalculationId = 10,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash
                            }
                        }
                    },
                    new FundingLine
                    {
                        TemplateLineId = 789,
                        Name = "FundingLine-4"
                    },
                }
            };

            _specificationsService.GetSpecificationSummaryById(SpecificationId)
                .Returns(new OkObjectResult(specificationSummary));

            _policiesApiClient.GetFundingTemplateContents(FundingStreamId, FundingPeriodId, TemplateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateMetadataContents));

            _calculationsApiClient.GetTemplateMapping(SpecificationId, FundingStreamId)
                .Returns(new ApiResponse<TemplateMapping>(HttpStatusCode.OK,
                    new TemplateMapping
                    {
                        FundingStreamId = FundingStreamId,
                        SpecificationId = SpecificationId,
                        TemplateMappingItems = new List<TemplateMappingItem>
                        {
                            new TemplateMappingItem
                            {
                                TemplateId = 7,
                                CalculationId = aValidCalculationId1
                            },
                            new TemplateMappingItem
                            {
                                TemplateId = 11,
                                CalculationId = aValidCalculationId2
                            },
                            new TemplateMappingItem
                            {
                                TemplateId = 8,
                                CalculationId = "CalculationIdForTemplateCalculationId2"
                            },
                            new TemplateMappingItem
                            {
                                TemplateId = 10,
                                CalculationId = missingTemplateMapping ? null : aValidCalculationId3
                            }
                        }
                    }));

            _calculationsApiClient.GetCalculationMetadataForSpecification(SpecificationId)
                .Returns(new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.OK,
                    new List<CalculationMetadata>
                    {
                        new CalculationMetadata
                        {
                            SpecificationId = SpecificationId,
                            CalculationId = aValidCalculationId1,
                            PublishStatus = CalculationExpectedPublishStatus
                        },
                        new CalculationMetadata
                        {
                            SpecificationId = SpecificationId,
                            CalculationId = aValidCalculationId2,
                            PublishStatus = CalculationExpectedPublishStatus
                        },
                        new CalculationMetadata
                        {
                            SpecificationId = SpecificationId,
                            CalculationId = aValidCalculationId3,
                            PublishStatus = CalculationExpectedPublishStatus
                        }
                    }));

            _graphApiClient.GetCircularDependencies(SpecificationId)
                .Returns(new ApiResponse<IEnumerable<Entity<Common.ApiClient.Graph.Models.Calculation>>>(
                    HttpStatusCode.OK, new List<Entity<Common.ApiClient.Graph.Models.Calculation>>()
                    {
                        new Entity<Common.ApiClient.Graph.Models.Calculation>()
                        {
                            Node = new Common.ApiClient.Graph.Models.Calculation()
                            {
                                SpecificationId = SpecificationId,
                                CalculationId = "CalculationIdForTemplateCalculationId2"
                            }
                        }
                    }));
        }

        private static List<FundingStructureItem> GetValidMappedFundingStructureItems()
        {
            List<FundingStructureItem> result = new List<FundingStructureItem>
            {
                new FundingStructureItem(1, "FundingLine-1", null, 123, null, null, FundingStructureType.FundingLine),
                new FundingStructureItem(1,
                    "FundingLine-2-withFundingLines",
                    null,
                    234,
                    null,
                    null,
                    FundingStructureType.FundingLine,
                    null,
                    new List<FundingStructureItem>
                    {
                        new FundingStructureItem(2, "FundingLine-2-fl-1", null, 345, null, null, FundingStructureType.FundingLine),
                        new FundingStructureItem(2,
                            "FundingLine-2-fl-2",
                            null,
                            456,
                            null,
                            null,
                            FundingStructureType.FundingLine,
                            null,
                            new List<FundingStructureItem>
                            {
                                new FundingStructureItem(3,
                                    "FundingLine-2-fl-2-fl-1",
                                    null,
                                    890,
                                    null,
                                    null,
                                    FundingStructureType.FundingLine)
                            })
                    }),
                new FundingStructureItem(1,
                    "FundingLine-3-withCalculationsAndFundingLines",
                    null,
                    567,
                    null,
                    "Error",
                    FundingStructureType.FundingLine,
                    null,
                    new List<FundingStructureItem>
                    {
                        new FundingStructureItem(
                            2,
                            "FundingLine-3-calc-1",
                            null,
                            7,
                            aValidCalculationId1,
                            CalculationExpectedPublishStatus.ToString(),
                            FundingStructureType.Calculation,
                            "Cash"),
                        new FundingStructureItem(
                            2,
                            "FundingLine-3-calc-2",
                            null,
                            11,
                            aValidCalculationId2,
                            "Error",
                            FundingStructureType.Calculation,
                            "Cash",
                            new List<FundingStructureItem>
                            {
                                new FundingStructureItem(
                                    3,
                                    "FundingLine-3-calc-2-calc-1",
                                    null,
                                    8,
                                    "CalculationIdForTemplateCalculationId2",
                                    "Error",
                                    FundingStructureType.Calculation,
                                    "Cash")
                            }),
                        new FundingStructureItem(
                            2,
                            "FundingLine-3-calc-3",
                            null,
                            10,
                            aValidCalculationId3,
                            CalculationExpectedPublishStatus.ToString(),
                            FundingStructureType.Calculation,
                            "Cash"),
                        new FundingStructureItem(
                            2,
                            "FundingLine-3-fl-1",
                            null,
                            678,
                            null,
                            null,
                            FundingStructureType.FundingLine)
                    }),
                new FundingStructureItem(1, "FundingLine-4", null, 789, null, null, FundingStructureType.FundingLine),
            };

            return result;
        }
    }
}