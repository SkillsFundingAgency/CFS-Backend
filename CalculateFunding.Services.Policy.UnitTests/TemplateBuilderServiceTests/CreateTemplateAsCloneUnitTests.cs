using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
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
    public class CreateTemplateAsCloneUnitTests
    {
        [TestClass]
        public class When_i_clone_template_to_create_new_template
        {
            TemplateCreateAsCloneCommand _command;
            TemplateBuilderService _service;
            Reference _author;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _sourceTemplate;
            private TemplateVersion _sourceTemplateVersion;
            private IFundingTemplateValidationService _templateValidationService;
            private ITemplateMetadataGenerator _templateMetadataGenerator;
            private ITemplateMetadataResolver _templateMetadataResolver;
            private IPolicyRepository _policyRepository;
            private ISearchRepository<TemplateIndex> _templateIndexer;

            public When_i_clone_template_to_create_new_template()
            {
                _command = new TemplateCreateAsCloneCommand
                {
                    CloneFromTemplateId = Guid.NewGuid().ToString(),
                    Description = "New Description",
                    FundingStreamId = "NEW",
                    FundingPeriodId = "20-21"
                };
                _author = new Reference("222", "CloningTestUser");

                SetupMocks();

                _service = new TemplateBuilderService(
                    _validatorFactory,
                    _templateValidationService,
                    _templateMetadataResolver,
                    _versionRepository,
                    _templateRepository,
                    _templateIndexer,
                    _policyRepository,
                    Substitute.For<ILogger>());

                _result = _service.CreateTemplateAsClone(_command, _author).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _sourceTemplateVersion = new TemplateVersion
                {
                    Name = "Old Test Name",
                    Description = "Old Description",
                    TemplateId = _command.CloneFromTemplateId,
                    TemplateJson = "{ \"Lorem\": \"ipsum\" }",
                    Version = 12,
                    FundingPeriodId = "19-20",
                    FundingStreamId = "OLD",
                    MinorVersion = 1,
                    MajorVersion = 0,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _sourceTemplate = new Template
                {
                    Name = _sourceTemplateVersion.Name,
                    TemplateId = _command.CloneFromTemplateId,
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
                    Current = _sourceTemplateVersion
                };

                _templateMetadataGenerator = Substitute.For<ITemplateMetadataGenerator>();
                _templateMetadataGenerator.Validate(Arg.Any<string>()).Returns(new ValidationResult());
                _templateMetadataResolver = Substitute.For<ITemplateMetadataResolver>();
                _templateMetadataResolver.GetService(Arg.Any<string>()).Returns(_templateMetadataGenerator);
                _templateValidationService = Substitute.For<IFundingTemplateValidationService>();
                _templateValidationService.ValidateFundingTemplate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), null).Returns(new FundingTemplateValidationResult { });
                _templateRepository.GetTemplate(Arg.Is(_command.CloneFromTemplateId)).Returns(_sourceTemplate);
                _templateRepository.GetAllTemplates().Returns(new [] {_sourceTemplate});
                _templateRepository.CreateDraft(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);

                _policyRepository = Substitute.For<IPolicyRepository>();
                _policyRepository.GetFundingPeriods().Returns(new [] { 
                    new FundingPeriod
                    {
                        Id = "2021",
                        Name = "Test Period",
                        Type = FundingPeriodType.FY
                    },
                    new FundingPeriod
                {
                    Id = _command.FundingPeriodId, 
                    Name = "Test Funding Period 2"
                }});
                _policyRepository.GetFundingStreams().Returns(new [] { 
                    new FundingStream
                    {
                        Id = "XX",
                        ShortName = "XX",
                        Name = "FundingSteam"
                    },
                    new FundingStream
                {
                    Id = _command.FundingStreamId,
                    Name = "Funding Stream 2",
                    ShortName = "Stream 2"
                }});
                _policyRepository.GetFundingConfigurations().Returns(new [] { new FundingConfiguration
                {
                    FundingStreamId = _command.FundingStreamId,
                    FundingPeriodId = _command.FundingPeriodId
                }});

                _templateIndexer = Substitute.For<ISearchRepository<TemplateIndex>>();
            }
            
            [TestMethod]
            public void Results_in_success()
            {
                _result.Succeeded.Should().BeTrue();
                _result.Exception.Should().BeNull();
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
            public void Saved_version_with_correct_name()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Name == $"{_command.FundingStreamId} {_command.FundingPeriodId}"));
            }

            [TestMethod]
            public void Saved_version_with_correct_description()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Description == _command.Description));
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingStreamId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.FundingStreamId == _command.FundingStreamId));
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingPeriodId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.FundingPeriodId == _command.FundingPeriodId));
            }

            [TestMethod]
            public void Saved_version_with_correct_SchemaVersion()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.SchemaVersion == _sourceTemplateVersion.SchemaVersion));
            }

            [TestMethod]
            public void Saved_version_with_correct_content()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateJson == _sourceTemplateVersion.TemplateJson));
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateId == _result.TemplateId));
            }

            [TestMethod]
            public void Saved_version_with_correct_status()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Status == TemplateStatus.Draft));
            }

            [TestMethod]
            public void Saved_template_with_a_current_version()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current != null));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_name()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Name == $"{_command.FundingStreamId} {_command.FundingPeriodId}"));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_description()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Description == _command.Description));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_FundingStreamId()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.FundingStreamId == _command.FundingStreamId));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_FundingPeriodId()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.FundingPeriodId == _command.FundingPeriodId));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_SchemaVersion()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.SchemaVersion == _sourceTemplateVersion.SchemaVersion));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_content()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.TemplateJson == _sourceTemplateVersion.TemplateJson));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Author.Name == _author.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Version == 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_minor_version_number()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.MinorVersion == 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_major_version_number()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.MajorVersion == 0));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_publish_status()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.PublishStatus == PublishStatus.Draft));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_status()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Status == TemplateStatus.Draft));
            }

            [TestMethod]
            public void Calls_template_indexer()
            {
                _templateIndexer.Received(1).Index(Arg.Any<IEnumerable<TemplateIndex>>());
            }
        }
    }
}