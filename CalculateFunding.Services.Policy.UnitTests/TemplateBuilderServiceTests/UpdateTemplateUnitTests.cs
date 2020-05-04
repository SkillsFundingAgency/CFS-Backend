using System;
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
    public class UpdateTemplateUnitTests
    {
        [TestClass]
        public class When_i_update_template_metadata
        {
            TemplateMetadataUpdateCommand _command;
            TemplateBuilderService _service;
            Reference _author;
            UpdateTemplateMetadataResponse _result;
            private IVersionRepository<TemplateVersion> _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionFirst;

            public When_i_update_template_metadata()
            {
                _command = new TemplateMetadataUpdateCommand
                {
                    Name = "New Test Template",
                    Description = "Lorem ipsum",
                    TemplateId = Guid.NewGuid().ToString()
                };
                _author = new Reference("222", "SecondTestUser");

                SetupMocks();
                
                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ILogger>());
                
                _result = _service.UpdateTemplateMetadata(_command, _author).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateRepository.IsTemplateNameInUse(Arg.Is(_command.Name)).Returns(false);
                _templateVersionFirst = new TemplateVersion
                {
                    Name = "Old Test Name",
                    Description = "Old Description",
                    TemplateId = _command.TemplateId,
                    Version = 0,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateBeforeUpdate = new Template
                {
                    Name = _command.Name,
                    TemplateId = _command.TemplateId,
                    Current = _templateVersionFirst
                };
                _templateRepository.GetTemplate(Arg.Is(_command.TemplateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<IVersionRepository<TemplateVersion>>();
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
            public void Saved_template_with_a_current_version()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current != null));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_name()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Name == _command.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_description()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Description == _command.Description));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Author.Name == _author.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Version == _templateVersionFirst.Version + 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_publish_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.PublishStatus == PublishStatus.Draft));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Status == TemplateStatus.Draft));
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
            public void Saved_version_with_correct_TemplateId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateId == _templateBeforeUpdate.TemplateId));
            }

            [TestMethod]
            public void Saved_version_with_correct_status()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Status == TemplateStatus.Draft));
            }
        }
        
        [TestClass]
        public class When_i_update_template_content
        {
            TemplateContentUpdateCommand _command;
            TemplateBuilderService _service;
            Reference _author;
            UpdateTemplateContentResponse _result;
            private IVersionRepository<TemplateVersion> _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionFirst;
            private IFundingTemplateValidationService _templateValidationService;
            private ITemplateMetadataGenerator _templateMetadataGenerator;
            private ITemplateMetadataResolver _templateMetadataResolver;

            public When_i_update_template_content()
            {
                _command = new TemplateContentUpdateCommand
                {
                    TemplateId = Guid.NewGuid().ToString(),
                    TemplateJson = "{ \"Lorem\": \"ipsum\" }"
                };
                _author = new Reference("222", "SecondTestUser");

                SetupMocks();
                
                _service = new TemplateBuilderService(
                    _validatorFactory,
                    _templateValidationService,
                    _templateMetadataResolver,
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ILogger>());
                
                _result = _service.UpdateTemplateContent(_command, _author).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateVersionFirst = new TemplateVersion
                {
                    Name = "Test Name",
                    Description = "Description",
                    TemplateId = _command.TemplateId,
                    TemplateJson = null,
                    Version = 0,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateBeforeUpdate = new Template
                {
                    Name = _templateVersionFirst.Name,
                    TemplateId = _command.TemplateId,
                    Current = _templateVersionFirst
                };

                _templateMetadataGenerator = Substitute.For<ITemplateMetadataGenerator>();
                _templateMetadataGenerator.Validate(Arg.Any<string>()).Returns(new ValidationResult());
                _templateMetadataResolver = Substitute.For<ITemplateMetadataResolver>();
                _templateMetadataResolver.GetService(Arg.Any<string>()).Returns(_templateMetadataGenerator);
                _templateValidationService = Substitute.For<IFundingTemplateValidationService>();
                _templateValidationService.ValidateFundingTemplate(Arg.Any<string>()).Returns(new FundingTemplateValidationResult { });
                _templateRepository.GetTemplate(Arg.Is(_command.TemplateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<IVersionRepository<TemplateVersion>>();
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);
            }

            [TestMethod]
            public void Results_in_success()
            {
                _result.Succeeded.Should().BeTrue();
                _result.Exception.Should().BeNull();
                _result.ValidationModelState.Should().BeNull();
            }

            [TestMethod]
            public void Validated_template_command()
            {
                _validatorFactory.Received(1).Validate(Arg.Is(_command));
            }

            [TestMethod]
            public void Saved_template_with_a_current_version()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current != null));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_json()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.TemplateJson == _command.TemplateJson));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_name()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Name == _templateVersionFirst.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_description()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Description == _templateVersionFirst.Description));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Author.Name == _author.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Version == _templateVersionFirst.Version + 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_publish_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.PublishStatus == PublishStatus.Draft));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Status == TemplateStatus.Draft));
            }

            [TestMethod]
            public void Saved_version_with_correct_name()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Name == _templateVersionFirst.Name));
            }

            [TestMethod]
            public void Saved_version_with_correct_description()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Description == _templateVersionFirst.Description));
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateId == _templateBeforeUpdate.TemplateId));
            }

            [TestMethod]
            public void Saved_version_with_correct_status()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Status == TemplateStatus.Draft));
            }
        }
    }
}