using System;
using System.Collections.Generic;
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
            private IPolicyRepository _policyRespository;
            private ISearchRepository<TemplateIndex> _templateIndexer;

            public When_i_clone_template_to_create_new_template()
            {
                _command = new TemplateCreateAsCloneCommand
                {
                    CloneFromTemplateId = Guid.NewGuid().ToString(),
                    Name = "New Name",
                    Description = "New Description",
                    FundingStreamId = "NEW",
                    FundingPeriodId = "NEW-2020"
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
                    _policyRespository,
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
                    Current = _sourceTemplateVersion
                };

                _templateMetadataGenerator = Substitute.For<ITemplateMetadataGenerator>();
                _templateMetadataGenerator.Validate(Arg.Any<string>()).Returns(new ValidationResult());
                _templateMetadataResolver = Substitute.For<ITemplateMetadataResolver>();
                _templateMetadataResolver.GetService(Arg.Any<string>()).Returns(_templateMetadataGenerator);
                _templateValidationService = Substitute.For<IFundingTemplateValidationService>();
                _templateValidationService.ValidateFundingTemplate(Arg.Any<string>()).Returns(new FundingTemplateValidationResult { });
                _templateRepository.GetTemplate(Arg.Is(_command.CloneFromTemplateId)).Returns(_sourceTemplate);
                _templateRepository.CreateDraft(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);

                _policyRespository = Substitute.For<IPolicyRepository>();
                _policyRespository.GetFundingPeriodById(Arg.Is(_command.FundingPeriodId)).Returns(new FundingPeriod
                {
                    Id = _command.FundingPeriodId, 
                    Name = "Test Funding Period"
                });
                _policyRespository.GetFundingStreamById(Arg.Is(_command.FundingStreamId)).Returns(new FundingStream
                {
                    Id = _command.FundingStreamId,
                    Name = "Funding Stream",
                    ShortName = "Stream"
                });

                _templateIndexer = Substitute.For<ISearchRepository<TemplateIndex>>();
            }
            
            [TestMethod]
            public void Results_in_success()
            {
                _result.Succeeded.Should().BeTrue();
                _result.Exception.Should().BeNull();
                _result.ValidationResult.Should().BeNull();
            }

            [TestMethod]
            public void Validated_template_command()
            {
                _validatorFactory.Received(1).Validate(Arg.Is(_command));
            }

            [TestMethod]
            public void Checked_for_duplicate_existing_template_name()
            {
                _templateRepository.Received(1).IsTemplateNameInUse(Arg.Is(_command.Name));
            }

            [TestMethod]
            public void Checked_for_duplicate_funding_stream_funding_period()
            {
                _templateRepository.Received(1).IsFundingStreamAndPeriodInUse(Arg.Is(_command.FundingStreamId), Arg.Is(_command.FundingPeriodId));
            }

            [TestMethod]
            public void Saved_version_with_correct_name()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Name == _command.Name));
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
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Name == _command.Name));
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