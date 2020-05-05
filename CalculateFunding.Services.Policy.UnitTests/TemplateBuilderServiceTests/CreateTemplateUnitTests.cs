using System.Linq;
using System.Net;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Interfaces;
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
    public class CreateTemplateUnitTests
    {
        [TestClass]
        public class When_i_create_initial_draft_template_without_content
        {
            TemplateCreateCommand _command;
            TemplateBuilderService _service;
            Reference _author;
            CreateTemplateResponse _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;

            public When_i_create_initial_draft_template_without_content()
            {
                _command = new TemplateCreateCommand
                {
                    Name = "Test Template",
                    Description = "Lorem ipsum",
                    FundingStreamId = "XXX",
                    SchemaVersion = "9.9"
                };
                _author = new Reference("111", "TestUser");

                SetupMocks();
                
                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ILogger>());
                
                _result = _service.CreateTemplate(_command, _author).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateRepository.IsTemplateNameInUse(Arg.Is(_command.Name)).Returns(false);
                _templateRepository.CreateDraft(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);
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
            public void Saved_version_with_correct_SchemaVersion()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.SchemaVersion == _command.SchemaVersion));
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
            public void Saved_current_version_with_correct_SchemaVersion()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.SchemaVersion == _command.SchemaVersion));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Author.Name == _author.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _templateRepository.Received(1).CreateDraft(Arg.Is<Template>(x => x.Current.Version == 0));
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
        }
        
        [TestClass]
        public class When_i_create_initial_draft_template_with_duplicate_name
        {
            TemplateCreateCommand _command;
            TemplateBuilderService _service;
            Reference _author;
            CreateTemplateResponse _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;

            public When_i_create_initial_draft_template_with_duplicate_name()
            {
                _command = new TemplateCreateCommand
                {
                    Name = "Test Template",
                    Description = "Lorem ipsum",
                    FundingStreamId = "XXX",
                    SchemaVersion = "9.9"
                };
                _author = new Reference("111", "TestUser");

                SetupMocks();
                
                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ILogger>());
                
                _result = _service.CreateTemplate(_command, _author).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateRepository.IsTemplateNameInUse(Arg.Is(_command.Name)).Returns(true);
                _templateRepository.CreateDraft(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);
            }

            [TestMethod]
            public void Results_in_failure()
            {
                _result.Succeeded.Should().BeFalse();
                _result.Exception.Should().BeNull();
                _result.ValidationResult.Should().NotBeNull();
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
            public void Did_not_save_template()
            {
                _templateRepository.Received(0).CreateDraft(Arg.Any<Template>());
            }

            [TestMethod]
            public void Did_not_save_version()
            {
                _versionRepository.Received(0).SaveVersion(Arg.Any<TemplateVersion>());
            }
        }
    }
}