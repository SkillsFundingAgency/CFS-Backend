using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces;
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
    public class GetTemplateUnitTests
    {
        [TestClass]
        public class When_i_request_current_version_of_template
        {
            private TemplateBuilderService _service;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private TemplateResponse _result;
            private TemplateVersion _templateVersion;
            private Template _template;

            public When_i_request_current_version_of_template()
            {
                _templateVersion = new TemplateVersion
                {
                    Name = "Test Name",
                    Description = "Description",
                    TemplateId = "123",
                    TemplateJson = null,
                    Version = 0,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser"),
                    FundingPeriodId = "12345"
                };
                _template = new Template
                {
                    TemplateId = _templateVersion.TemplateId,
                    Name = _templateVersion.Name,
                    Current = _templateVersion
                };
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateRepository.GetTemplate(Arg.Is(_template.TemplateId)).Returns(_template);

                _service = new TemplateBuilderService(
                    Substitute.For<IIoCValidatorFactory>(),
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    Substitute.For<ITemplateVersionRepository>(),
                    _templateRepository,
                    Substitute.For<ISearchRepository<TemplateIndex>>(),
                    Substitute.For<IPolicyRepository>(),
                    Substitute.For<ILogger>());

                _result = _service.GetTemplate(_template.TemplateId).GetAwaiter().GetResult();
            }

            [TestMethod]
            public void Results_in_success()
            {
                _result.Should().NotBeNull();
            }

            [TestMethod]
            public void Returns_correct_TemplateId()
            {
                _result.TemplateId.Should().Be(_templateVersion.TemplateId);
            }

            [TestMethod]
            public void Returns_correct_Name()
            {
                _result.Name.Should().Be(_templateVersion.Name);
            }

            [TestMethod]
            public void Returns_correct_Description()
            {
                _result.Description.Should().Be(_templateVersion.Description);
            }

            [TestMethod]
            public void Returns_correct_SchemaVersion()
            {
                _result.SchemaVersion.Should().Be(_templateVersion.SchemaVersion);
            }

            [TestMethod]
            public void Returns_correct_Version()
            {
                _result.Version.Should().Be(_templateVersion.Version);
            }

            [TestMethod]
            public void Returns_correct_FundingPeriodId()
            {
                _result.FundingPeriodId.Should().Be(_templateVersion.FundingPeriodId);
            }

            [TestMethod]
            public void Returns_correct_TemplateJson()
            {
                _result.TemplateJson.Should().Be(_templateVersion.TemplateJson);
            }

            [TestMethod]
            public void Returns_correct_FundingStreamId()
            {
                _result.FundingStreamId.Should().Be(_templateVersion.FundingStreamId);
            }

            [TestMethod]
            public void Returns_correct_Comment()
            {
                _result.Comments.Should().Be(_templateVersion.Comment);
            }

            [TestMethod]
            public void Returns_correct_Status()
            {
                _result.Status.Should().Be(_templateVersion.Status);
            }

            [TestMethod]
            public void Returns_correct_PublishStatus()
            {
                _result.PublishStatus.Should().Be(_templateVersion.PublishStatus);
            }

            [TestMethod]
            public void Returns_correct_LastModificationDate()
            {
                _result.LastModificationDate.Should().Be(_templateVersion.Date.DateTime);
            }

            [TestMethod]
            public void Returns_correct_AuthorName()
            {
                _result.AuthorName.Should().Be(_templateVersion.Author.Name);
            }

            [TestMethod]
            public void Returns_correct_AuthorId()
            {
                _result.AuthorId.Should().Be(_templateVersion.Author.Id);
            }
        }

        [TestClass]
        public class When_i_request_previous_version_of_template
        {
            private TemplateBuilderService _service;
            private ITemplateVersionRepository _templateVersionRepository;
            private IIoCValidatorFactory _validatorFactory;
            private TemplateResponse _result;
            private TemplateVersion _templateVersionPrevious;
            private TemplateVersion _templateVersionCurrent;
            private Template _template;

            public When_i_request_previous_version_of_template()
            {
                _templateVersionPrevious = new TemplateVersion
                {
                    Name = "Test Name 1",
                    Description = "Description 1",
                    TemplateId = "123",
                    TemplateJson = null,
                    Version = 0,
                    SchemaVersion = "1.1",
                    FundingPeriodId = "12345",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateVersionCurrent = new TemplateVersion
                {
                    Name = "Test Name 2",
                    Description = "Description 2",
                    TemplateId = "123",
                    TemplateJson = "{ \"Lorem\": \"ipsum\" }",
                    Version = 1,
                    SchemaVersion = "1.1",
                    FundingPeriodId = "12345",
                    Status = TemplateStatus.Published,
                    Author = new Reference("222", "SecondTestUser")
                };
                _template = new Template
                {
                    TemplateId = _templateVersionCurrent.TemplateId,
                    Name = _templateVersionCurrent.Name,
                    Current = _templateVersionCurrent
                };
                _templateVersionRepository = Substitute.For<ITemplateVersionRepository>();
                _templateVersionRepository.GetTemplateVersion(
                    Arg.Is(_templateVersionPrevious.TemplateId),
                    Arg.Is(_templateVersionPrevious.Version)).Returns(_templateVersionPrevious);

                _service = new TemplateBuilderService(
                    Substitute.For<IIoCValidatorFactory>(),
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _templateVersionRepository,
                    Substitute.For<ITemplateRepository>(),
                    Substitute.For<ISearchRepository<TemplateIndex>>(),
                    Substitute.For<IPolicyRepository>(),
                    Substitute.For<ILogger>());

                _result = _service
                    .GetTemplateVersion(_templateVersionPrevious.TemplateId, _templateVersionPrevious.Version.ToString())
                    .GetAwaiter()
                    .GetResult();
            }

            [TestMethod]
            public void Results_in_success()
            {
                _result.Should().NotBeNull();
            }

            [TestMethod]
            public void Returns_correct_TemplateId()
            {
                _result.TemplateId.Should().Be(_templateVersionPrevious.TemplateId);
            }

            [TestMethod]
            public void Returns_correct_Name()
            {
                _result.Name.Should().Be(_templateVersionPrevious.Name);
            }

            [TestMethod]
            public void Returns_correct_Description()
            {
                _result.Description.Should().Be(_templateVersionPrevious.Description);
            }

            [TestMethod]
            public void Returns_correct_SchemaVersion()
            {
                _result.SchemaVersion.Should().Be(_templateVersionPrevious.SchemaVersion);
            }

            [TestMethod]
            public void Returns_correct_Version()
            {
                _result.Version.Should().Be(_templateVersionPrevious.Version);
            }

            [TestMethod]
            public void Returns_correct_FundingPeriodId()
            {
                _result.FundingPeriodId.Should().Be(_templateVersionPrevious.FundingPeriodId);
            }

            [TestMethod]
            public void Returns_correct_TemplateJson()
            {
                _result.TemplateJson.Should().Be(_templateVersionPrevious.TemplateJson);
            }

            [TestMethod]
            public void Returns_correct_FundingStreamId()
            {
                _result.FundingStreamId.Should().Be(_templateVersionPrevious.FundingStreamId);
            }

            [TestMethod]
            public void Returns_correct_Comment()
            {
                _result.Comments.Should().Be(_templateVersionPrevious.Comment);
            }

            [TestMethod]
            public void Returns_correct_Status()
            {
                _result.Status.Should().Be(_templateVersionPrevious.Status);
            }

            [TestMethod]
            public void Returns_correct_PublishStatus()
            {
                _result.PublishStatus.Should().Be(_templateVersionPrevious.PublishStatus);
            }

            [TestMethod]
            public void Returns_correct_LastModificationDate()
            {
                _result.LastModificationDate.Should().Be(_templateVersionPrevious.Date.DateTime);
            }

            [TestMethod]
            public void Returns_correct_AuthorName()
            {
                _result.AuthorName.Should().Be(_templateVersionPrevious.Author.Name);
            }

            [TestMethod]
            public void Returns_correct_AuthorId()
            {
                _result.AuthorId.Should().Be(_templateVersionPrevious.Author.Id);
            }
        }
    }
}