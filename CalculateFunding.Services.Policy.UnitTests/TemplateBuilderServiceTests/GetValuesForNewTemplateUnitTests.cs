using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Validators;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Policy.TemplateBuilderServiceTests
{
    public class GetValuesForNewTemplateUnitTests
    {
        [TestClass]
        public class When_i_request_all_funding_stream_periods_not_in_use
        {
            private TemplateBuilderService _service;
            private IEnumerable<FundingStreamWithPeriods> _result;
            private ITemplateRepository _templateRepository;
            private IPolicyRepository _policyRepository;
            private Template _existingTemplate;

            public When_i_request_all_funding_stream_periods_not_in_use()
            {
                SetupMocks();
                
                _service = new TemplateBuilderService(
                    Substitute.For<IIoCValidatorFactory>(),
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    Substitute.For<ITemplateVersionRepository>(),
                    _templateRepository,
                    Substitute.For<ISearchRepository<TemplateIndex>>(),
                    _policyRepository,
                    Substitute.For<ITemplateBlobService>(),
                    Substitute.For<ILogger>());
                
                _result = _service.GetFundingStreamAndPeriodsWithoutTemplates().GetAwaiter().GetResult();
            }
                        
            private void SetupMocks()
            {
                _templateRepository = Substitute.For<ITemplateRepository>();
                _existingTemplate = new Template
                {
                    Name = "Test",
                    TemplateId = Guid.NewGuid().ToString(),
                    FundingPeriod = new FundingPeriod
                    {
                        Id = "2021",
                        Name = "Test Period",
                        Type = FundingPeriodType.FY
                    },
                    FundingStream = new FundingStream
                    {
                        Id = "XXX",
                        ShortName = "XXX",
                        Name = "FundingSteam"
                    }
                };
                
                var version = new TemplateVersion
                {
                    Name = "Old Test Name",
                    TemplateId = _existingTemplate.TemplateId,
                    TemplateJson = @"{""$schema"":""https://fundingschemas.blob.core.windows.net/schemas/funding-template-schema-1.1.json"",""schemaVersion"":""1.1"",""fundingTemplate"":{""fundingLines"":[{""templateLineId"":1,""type"":""Payment"",""name"":""Funding Line 1"",""fundingLineCode"":""DSG-001"",""fundingLines"":[],""calculations"":[]}],""fundingPeriod"":{""id"":""XX-2021"",""period"":""2021"",""name"":""XX-2021"",""type"":""FY"",""startDate"":""2020-04-01T00:00:00+00:00"",""endDate"":""2021-03-31T00:00:00+00:00""},""fundingStream"":{""code"":""DSG"",""name"":""DSG""},""fundingTemplateVersion"":""0.1""}}",
                    Version = 1,
                    MinorVersion = 1,
                    MajorVersion = 0,
                    FundingPeriodId = _existingTemplate.FundingPeriod.Id,
                    FundingStreamId = _existingTemplate.FundingStream.Id,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Published,
                    Author = new Reference("111", "FirstTestUser")
                };
                _existingTemplate.Current = version;
                
                _templateRepository.GetAllTemplates().Returns(new List<Template> { _existingTemplate});

                _policyRepository = Substitute.For<IPolicyRepository>();
                var fundingConfigurations = new List<FundingConfiguration>
                {
                    new FundingConfiguration
                    {
                        FundingStreamId = "XX",
                        FundingPeriodId = "2021"
                    },
                    new FundingConfiguration
                    {
                        FundingStreamId = "XX",
                        FundingPeriodId = "2122"
                    },
                    new FundingConfiguration
                    {
                        FundingStreamId = "XXX",
                        FundingPeriodId = "2021"
                    },
                    new FundingConfiguration
                    {
                        FundingStreamId = "XXX",
                        FundingPeriodId = "2122"
                    }
                };
                _policyRepository.GetFundingConfigurations().Returns(fundingConfigurations);

                _policyRepository.GetFundingStreams().Returns(new List<FundingStream>
                {
                    new FundingStream
                    {
                        Id = "XX",
                        ShortName = "XX",
                        Name = "FundingSteam XX"
                    },
                    new FundingStream
                    {
                        Id = "XXX",
                        ShortName = "XXX",
                        Name = "FundingSteam XXX"
                    }
                });
                _policyRepository.GetFundingPeriods().Returns(new List<FundingPeriod>
                {
                    new FundingPeriod
                    {
                        Id = "2021",
                        Name = "Test Period 20-21",
                        Type = FundingPeriodType.FY
                    },
                    new FundingPeriod
                    {
                        Id = "2122",
                        Name = "Test Period 21-22",
                        Type = FundingPeriodType.FY
                    }
                });
            }
            

            [TestMethod]
            public void Returns_non_empty_result()
            {
                _result.Should().NotBeNull().And.NotBeEmpty();
            }

            [TestMethod]
            public void Returns_correct_funding_stream()
            {
                _result.Should().ContainSingle(x => x.FundingStream.Id == "XX")
                    .And.ContainSingle(x => x.FundingStream.Id == "XXX");
            }

            [TestMethod]
            public void Does_not_return_funding_stream_period_already_in_use()
            {
                _result.SingleOrDefault(x => x.FundingStream.Id == _existingTemplate.FundingStream.Id)?
                    .FundingPeriods.Should()
                    .NotContain(x => x.Id == _existingTemplate.FundingPeriod.Id);
            }

            [TestMethod]
            public void Returns_funding_stream_period_not_already_in_use()
            {
                _result.SingleOrDefault(x => x.FundingStream.Id == _existingTemplate.FundingStream.Id)?
                    .FundingPeriods.Should()
                    .ContainSingle(x => x.Id == "2122");
            }

            [TestMethod]
            public void Returns_correct_funding_stream_periods()
            {
                _result.SingleOrDefault(x => x.FundingStream.Id == "XX")?
                    .FundingPeriods.Should()
                        .Contain(x => x.Id == "2021")
                        .And.Contain(x => x.Id == "2122");
            }
        }
    }
}