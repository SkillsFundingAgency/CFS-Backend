using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy;
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
    public class FindTemplateVersionsUnitTests
    {
        [TestClass]
        public class When_i_request_draft_versions_of_templates_by_funding_stream_and_period
        {
            private readonly TemplateBuilderService _service;
            private readonly ITemplateRepository _templateRepository;
            private readonly ITemplateVersionRepository _templateVersionRepository;
            private readonly IEnumerable<TemplateSummaryResponse> _results;
            private readonly TemplateVersion _templateVersionPrevious;
            private readonly TemplateVersion _templateVersionCurrent;
            private Template _template;
            private string _templateId;

            public When_i_request_draft_versions_of_templates_by_funding_stream_and_period()
            {
                _templateId = Guid.NewGuid().ToString();
                _templateVersionPrevious = new TemplateVersion
                {
                    Name = "Test Name 1",
                    TemplateId = _templateId,
                    Version = 1,
                    MajorVersion = 1,
                    MinorVersion = 0,
                    SchemaVersion = "1.1",
                    FundingPeriodId = "12345",
                    Status = TemplateStatus.Draft,
                    Author = new Reference
                    {
                        Id = "111",
                        Name = "Test 111"
                    }
                };
                _templateVersionCurrent = new TemplateVersion
                {
                    Name = "Test Name 2",
                    TemplateId = _templateId,
                    Version = 2,
                    MajorVersion = 2,
                    MinorVersion = 0,
                    SchemaVersion = "1.1",
                    FundingPeriodId = "12345",
                    Status = TemplateStatus.Draft,
                    Author = new Reference
                    {
                        Id = "222",
                        Name = "Test 222"
                    }
                };
                _template = new Template
                {
                    Name = _templateVersionPrevious.Name,
                    TemplateId = _templateId,
                    FundingPeriod = new FundingPeriod
                    {
                        Id = "2021",
                        Name = "Test Period",
                        Type = FundingPeriodType.FY
                    },
                    FundingStream = new FundingStream
                    {
                        Id = "XX",
                        ShortName = "XX",
                        Name = "FundingSteam"
                    },
                    Current = _templateVersionCurrent
                };
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateRepository.GetTemplate(Arg.Is(_templateId)).Returns(_template);
                _templateVersionRepository = Substitute.For<ITemplateVersionRepository>();
                _templateVersionRepository.FindByFundingStreamAndPeriod(Arg.Any<FindTemplateVersionQuery>())
                    .Returns(new []{_templateVersionPrevious, _templateVersionCurrent});
                
                _service = new TemplateBuilderService(
                    Substitute.For<IIoCValidatorFactory>(),
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _templateVersionRepository,
                    _templateRepository,
                    Substitute.For<ISearchRepository<TemplateIndex>>(),
                    Substitute.For<IPolicyRepository>(),
                    Substitute.For<ITemplateBlobService>(),
                    Substitute.For<ILogger>());
                
                _results = _service
                    .FindVersionsByFundingStreamAndPeriod(new FindTemplateVersionQuery
                    {
                        FundingStreamId = "XXX",
                        FundingPeriodId = "2021",
                        Statuses = new List<TemplateStatus> {TemplateStatus.Draft}
                    })
                    .GetAwaiter()
                    .GetResult();
            }

            [TestMethod]
            public void Results_in_success()
            {
                _results.Should().NotBeNull();
            }

            [TestMethod]
            public void Returns_expected_number_of_results()
            {
                _results.Should().HaveCount(2);
            }

            [TestMethod]
            public void Returns_correct_template_version()
            {
                _results.First().Version.Should().Be(_templateVersionPrevious.Version);
            }

            [TestMethod]
            public void Returns_correct_Status()
            {
                _results.Should().Match(x => 
                    x.All(version => version.Status == TemplateStatus.Draft));
            }

            [TestMethod]
            public void Returns_correct_IsCurrentVersion()
            {
                _results.Single(x => x.Version == _template.Current.Version).IsCurrentVersion.Should().BeTrue();
                _results.Where(x => x.Version != _template.Current.Version)
                    .Should().Match(x => x
                        .All(t => t.IsCurrentVersion == false));
            }
        }

    }
}