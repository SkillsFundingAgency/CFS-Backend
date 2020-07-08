using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Policy.TemplateBuilderServiceTests
{
    [TestClass]
    public class RestoreTemplateUnitTests
    {
        TemplateFundingLinesUpdateCommand _command;
        TemplateBuilderService _service;
        Reference _author;
        CommandResult _result;
        private ITemplateVersionRepository _versionRepository;
        private ITemplateRepository _templateRepository;
        private IIoCValidatorFactory _validatorFactory;
        private Template _templateBeforeRestore;
        private TemplateVersion _templateVersionBeforeRestore;
        private IFundingTemplateValidationService _templateValidationService;
        private ITemplateMetadataGenerator _templateMetadataGenerator;
        private ITemplateMetadataResolver _templateMetadataResolver;
        private IPolicyRepository _policyRepository;
        private ISearchRepository<TemplateIndex> _searchRepository;
        private Template _restoredTemplate;
        private TemplateVersion _restoredTemplateVersion;
        private ITemplateBlobService _templateBlobService;

        public RestoreTemplateUnitTests()
        {
            _command = new TemplateFundingLinesUpdateCommand
            {
                TemplateId = Guid.NewGuid().ToString(),
                TemplateFundingLinesJson = @"[{""templateLineId"":1,""type"":""Payment"",""name"":""Funding Line 1"",""fundingLineCode"":""DSG-001"",""fundingLines"":[],""calculations"":[]}]"
            };
            _author = new Reference("222", "SecondTestUser");

            SetupMocks();

            _service = new TemplateBuilderService(
                _validatorFactory,
                _templateValidationService,
                _templateMetadataResolver,
                _versionRepository,
                _templateRepository,
                _searchRepository,
                _policyRepository,
                _templateBlobService,
                Substitute.For<ILogger>());

            _result = _service.RestoreTemplateContent(_command, _author).GetAwaiter().GetResult();
        }

        private void SetupMocks()
        {
            _validatorFactory = Substitute.For<IIoCValidatorFactory>();
            _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
            _templateRepository = Substitute.For<ITemplateRepository>();
            _templateVersionBeforeRestore = new TemplateVersion
            {
                Name = "XXX 20-21",
                TemplateId = _command.TemplateId,
                TemplateJson = null,
                Version = 1,
                MinorVersion = 1,
                MajorVersion = 0,
                SchemaVersion = "1.1",
                FundingStreamId = "XX",
                FundingPeriodId = "20-21",
                Status = TemplateStatus.Published,
                Author = new Reference("111", "FirstTestUser")
            };
            _templateBeforeRestore = new Template
            {
                Name = "Template Name",
                Description = "Description",
                TemplateId = _command.TemplateId,
                FundingPeriod = new FundingPeriod
                {
                    Id = "20-21",
                    Name = "Test Period",
                    Type = FundingPeriodType.FY
                },
                FundingStream = new FundingStream
                {
                    Id = "XX",
                    ShortName = "XX",
                    Name = "FundingSteam"
                },
                Current = _templateVersionBeforeRestore
            };

            _templateMetadataGenerator = Substitute.For<ITemplateMetadataGenerator>();
            _templateMetadataGenerator.Validate(Arg.Any<string>()).Returns(new ValidationResult());
            _templateMetadataResolver = Substitute.For<ITemplateMetadataResolver>();
            _templateMetadataResolver.GetService(Arg.Any<string>()).Returns(_templateMetadataGenerator);
            _templateValidationService = Substitute.For<IFundingTemplateValidationService>();
            _templateValidationService.ValidateFundingTemplate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), null).Returns(new FundingTemplateValidationResult { });
            _templateRepository.GetTemplate(Arg.Is(_command.TemplateId)).Returns(_templateBeforeRestore);
            _templateRepository.Update(Arg.Do<Template>(x => _restoredTemplate = x)).Returns(HttpStatusCode.OK);

            _versionRepository = Substitute.For<ITemplateVersionRepository>();
            _versionRepository.SaveVersion(Arg.Do<TemplateVersion>(x => _restoredTemplateVersion = x)).Returns(HttpStatusCode.OK);

            _searchRepository = Substitute.For<ISearchRepository<TemplateIndex>>();
            _searchRepository.Index(Arg.Any<IEnumerable<TemplateIndex>>()).Returns(Enumerable.Empty<IndexError>());

            _policyRepository = Substitute.For<IPolicyRepository>();
            _policyRepository.GetFundingPeriodById(Arg.Any<string>()).Returns(new FundingPeriod
            {
                Id = "2021",
                Name = "Test Period",
                Type = FundingPeriodType.FY
            });
            _policyRepository.GetFundingStreamById(Arg.Any<string>()).Returns(new FundingStream
            {
                Id = "XX",
                ShortName = "XX",
                Name = "FundingSteam"
            });
            _templateBlobService = Substitute.For<ITemplateBlobService>();
            _templateBlobService.PublishTemplate(Arg.Any<Template>()).Returns(CommandResult.Success());
        }

        [TestMethod]
        public void Results_in_success()
        {
            _result.Succeeded.Should().BeTrue();
            _result.Exception.Should().BeNull();
            _result.Version.Should().Be(_templateVersionBeforeRestore.Version + 1);
        }

        [TestMethod]
        public void No_validation_errors()
        {
            (_result.ValidationResult == null || _result.ValidationResult.IsValid).Should()
                .BeTrue($"Unexpected validation errors: {_result.ValidationResult?.Errors.Select(x => x.ErrorMessage).Aggregate((x, y) => x + ", " + y)}");
        }

        [TestMethod]
        public void Validated_template_command()
        {
            _validatorFactory.Received(1).Validate(Arg.Is(_command));
        }

        [TestMethod]
        public void Restored_template_with_a_current_version()
        {
            _restoredTemplate?.Current.Should().NotBeNull();
        }

        [TestMethod]
        public void Restored_current_version_with_correct_json()
        {
            _restoredTemplate?.Current?.TemplateJson.Should().Contain(_command.TemplateFundingLinesJson);
        }

        [TestMethod]
        public void Restored_current_version_with_correct_name()
        {
            _restoredTemplate?.Current?.Name.Should().Be(_templateBeforeRestore.Name);
        }

        [TestMethod]
        public void Restored_current_version_with_correct_author()
        {
            _restoredTemplate?.Current?.Author?.Name.Should().Be(_author.Name);
            _restoredTemplate?.Current?.Author?.Id.Should().Be(_author.Id);
        }

        [TestMethod]
        public void Restored_current_version_with_correct_version_number()
        {
            _restoredTemplate?.Current?.Version.Should().Be(_templateVersionBeforeRestore.Version + 1);
        }

        [TestMethod]
        public void Restored_current_version_with_correct_minor_version_number()
        {
            _restoredTemplate?.Current?.MinorVersion.Should().Be(_templateVersionBeforeRestore.MinorVersion + 1);
        }

        [TestMethod]
        public void Restored_current_version_with_correct_major_version_number()
        {
            _restoredTemplate?.Current?.MajorVersion.Should().Be(_templateVersionBeforeRestore.MajorVersion);
        }

        [TestMethod]
        public void Restored_current_version_with_correct_publish_status()
        {
            _restoredTemplate?.Current?.PublishStatus.Should().Be(PublishStatus.Draft);
        }

        [TestMethod]
        public void Restored_current_version_with_correct_status()
        {
            _restoredTemplate?.Current?.Status.Should().Be(TemplateStatus.Draft);
        }

        [TestMethod]
        public void Restored_version_with_correct_name()
        {
            _restoredTemplateVersion?.Name.Should().Be(_templateBeforeRestore.Name);
        }

        [TestMethod]
        public void Restored_version_with_correct_TemplateId()
        {
            _restoredTemplateVersion?.TemplateId.Should().Be(_templateBeforeRestore.TemplateId);
        }

        [TestMethod]
        public void Restored_version_with_correct_status()
        {
            _restoredTemplateVersion?.Status.Should().Be(TemplateStatus.Draft);
        }

        [TestMethod]
        public void Restored_version_with_blank_comment()
        {
            _restoredTemplateVersion?.Comment.Should().BeNullOrEmpty();
        }

        [TestMethod]
        public void Restored_version_with_recent_date()
        {
            _restoredTemplateVersion?.Date.Should().BeAfter(DateTimeOffset.Now.AddMinutes(-1));
        }

        [TestMethod]
        public void Restored_version_with_correct_FundingPeriodId()
        {
            _restoredTemplateVersion?.FundingPeriodId.Should().Be(_templateVersionBeforeRestore.FundingPeriodId);
        }

        [TestMethod]
        public void Restored_version_with_correct_FundingStreamId()
        {
            _restoredTemplateVersion?.FundingStreamId.Should().Be(_templateVersionBeforeRestore.FundingStreamId);
        }
    }
}