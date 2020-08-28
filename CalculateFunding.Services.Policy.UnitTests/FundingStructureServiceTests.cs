using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Validators;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Policy
{
    [TestClass]
    public class FundingStructureServiceTests
    {
        //this is lifted from the FE project pretty much as is

        private const string FundingStreamId = "DSG";
        private const string FundingPeriodId = "AY-2021";
        private const string TemplateVersion = "1.0";
        private const string SpecificationId = "680898bd-9ddc-4d11-9913-2a2aa34f213c";
        private const string CalculationId = "aValidCalculationId";

        private const PublishStatus CalculationExpectedPublishStatus = PublishStatus.Approved;

        private ISpecificationsApiClient _specificationsApiClient;
        private ICalculationsApiClient _calculationsApiClient;
        private IResultsApiClient _resultsApiClient;
        private ICacheProvider _cacheProvider;
        private IFundingTemplateService _fundingTemplateService;
        private IValidator<UpdateFundingStructureLastModifiedRequest> _validator;
        
        private FundingStructureService _service;

        [TestInitialize]
        public void SetUp()
        {
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();
            _resultsApiClient = Substitute.For<IResultsApiClient>();
            _cacheProvider = Substitute.For<ICacheProvider>();
            _fundingTemplateService = Substitute.For<IFundingTemplateService>();
            _validator = Substitute.For<IValidator<UpdateFundingStructureLastModifiedRequest>>();

            _service = new FundingStructureService(
                _cacheProvider,
                _specificationsApiClient,
                _calculationsApiClient,
                _resultsApiClient,
                _fundingTemplateService,
                _validator,
                new PolicyResiliencePolicies
                {
                    CacheProvider = Polly.Policy.NoOpAsync(),
                    SpecificationsApiClient = Polly.Policy.NoOpAsync(),
                    CalculationsApiClient = Polly.Policy.NoOpAsync(),
                    ResultsApiClient = Polly.Policy.NoOpAsync()
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
                .BeEquivalentTo(new [] {  failureOne.ErrorMessage });
            
            serializableError[failureTwo.PropertyName]
                .Should()
                .BeEquivalentTo(new [] {  failureTwo.ErrorMessage });

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

           await  AndTheLastUpdatedCacheWasUpdated($"{CacheKeys.FundingLineStructureTimestamp}{request.SpecificationId}:{request.FundingStreamId}:{request.FundingPeriodId}",
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
            => _cacheProvider.GetAsync<DateTimeOffset?>($"{CacheKeys.FundingLineStructureTimestamp}{specificationId}:{fundingStreamId}:{fundingPeriodId}")
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
        public async Task GetFundingStructures_ReturnsFlatStructureWithCorrectLevelsAndInCorrectOrder()
        {
            ValidScenarioSetup(FundingStreamId);

            IActionResult apiResponseResult = await _service.GetFundingStructure(FundingStreamId, FundingPeriodId, SpecificationId);

            List<FundingStructureItem> expectedFundingStructureItems = GetValidMappedFundingStructureItems();
            apiResponseResult.Should().BeOfType<OkObjectResult>();
            OkObjectResult typedResult = apiResponseResult as OkObjectResult;
            FundingStructure fundingStructureItems = typedResult?.Value as FundingStructure;
            fundingStructureItems?.Items.Count().Should().Be(3);
            fundingStructureItems?.Items.Should().BeEquivalentTo(expectedFundingStructureItems);
        }

        [TestMethod]
        public async Task GetFundingStructures_ThrowsInternalErrorIfTemplateIdNotSet()
        {
            ValidScenarioSetup(FundingStreamId.ToLowerInvariant());

            IActionResult apiResponseResult = await _service.GetFundingStructure(FundingStreamId, FundingPeriodId, SpecificationId);

            apiResponseResult.Should().BeOfType<InternalServerErrorResult>();
        }

        private void ValidScenarioSetup(string fundingStreamId)
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
                        Name = "FundingLine-1"
                    },
                    new FundingLine
                    {
                        Name = "FundingLine-2-withFundingLines",
                        FundingLines = new List<FundingLine>
                        {
                            new FundingLine
                            {
                                Name = "FundingLine-2-fl-1"
                            },
                            new FundingLine
                            {
                                Name = "FundingLine-2-fl-2",
                                FundingLines = new List<FundingLine>
                                {
                                    new FundingLine
                                    {
                                        Name = "FundingLine-2-fl-2-fl-1"
                                    }
                                }
                            }
                        }
                    },
                    new FundingLine
                    {
                        Name = "FundingLine-3-withCalculationsAndFundingLines",
                        FundingLines = new List<FundingLine>
                        {
                            new FundingLine
                            {
                                Name = "FundingLine-3-fl-1"
                            }
                        },
                        Calculations = new List<Calculation>
                        {
                            new Calculation
                            {
                                Name = "FundingLine-3-calc-1",
                                TemplateCalculationId = 1
                            },
                            new Calculation
                            {
                                Name = "FundingLine-3-calc-2",
                                TemplateCalculationId = 1,
                                Calculations = new List<Calculation>
                                {
                                    new Calculation
                                    {
                                        Name = "FundingLine-3-calc-2-calc-1",
                                        TemplateCalculationId = 2
                                    }
                                }
                            }
                        }
                    }
                }
            };

            _specificationsApiClient.GetSpecificationSummaryById(SpecificationId)
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            _fundingTemplateService.GetFundingTemplateContents(FundingStreamId, FundingPeriodId, TemplateVersion)
                .Returns(new OkObjectResult(templateMetadataContents));

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
                                TemplateId = 1,
                                CalculationId = CalculationId
                            },
                            new TemplateMappingItem
                            {
                                TemplateId = 2,
                                CalculationId = "CalculationIdForTemplateCalculationId2"
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
                            CalculationId = CalculationId,
                            PublishStatus = CalculationExpectedPublishStatus
                        }
                    }));
        }

        private static List<FundingStructureItem> GetValidMappedFundingStructureItems()
        {
            List<FundingStructureItem> result = new List<FundingStructureItem>
            {
                new FundingStructureItem(1, "FundingLine-1", null, null, FundingStructureType.FundingLine),
                new FundingStructureItem(1,
                    "FundingLine-2-withFundingLines",
                    null,
                    null,
                    FundingStructureType.FundingLine,
                    null,
                    new List<FundingStructureItem>
                    {
                        new FundingStructureItem(2, "FundingLine-2-fl-1", null, null, FundingStructureType.FundingLine),
                        new FundingStructureItem(2,
                            "FundingLine-2-fl-2",
                            null,
                            null,
                            FundingStructureType.FundingLine,
                            null,
                            new List<FundingStructureItem>
                            {
                                new FundingStructureItem(3,
                                    "FundingLine-2-fl-2-fl-1",
                                    null,
                                    null,
                                    FundingStructureType.FundingLine)
                            })
                    }),
                new FundingStructureItem(1,
                    "FundingLine-3-withCalculationsAndFundingLines",
                    null,
                    null,
                    FundingStructureType.FundingLine,
                    null,
                    new List<FundingStructureItem>
                    {
                        new FundingStructureItem(
                            2,
                            "FundingLine-3-calc-1",
                            CalculationId,
                            CalculationExpectedPublishStatus.ToString(),
                            FundingStructureType.Calculation),
                        new FundingStructureItem(
                            2,
                            "FundingLine-3-calc-2",
                            CalculationId,
                            CalculationExpectedPublishStatus.ToString(),
                            FundingStructureType.Calculation,
                            null,
                            new List<FundingStructureItem>
                            {
                                new FundingStructureItem(
                                    3,
                                    "FundingLine-3-calc-2-calc-1",
                                    "CalculationIdForTemplateCalculationId2",
                                    null,
                                    FundingStructureType.Calculation)
                            }),
                        new FundingStructureItem(
                            2,
                            "FundingLine-3-fl-1",
                            null,
                            null,
                            FundingStructureType.FundingLine)
                    })
            };

            return result;
        }
    }
}