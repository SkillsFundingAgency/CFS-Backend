using System;
using System.Linq;
using System.Net;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Models.Versioning;
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
    public class ApproveTemplateUnitTests
    {
        [TestClass]
        public class When_i_approve_current_template
        {
            TemplateBuilderService _service;
            Reference _author;
            string _templateId;
            string _comment;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionBeforeUpdate;

            public When_i_approve_current_template()
            {
                _templateId = Guid.NewGuid().ToString();
                _author = new Reference("222", "SecondTestUser");
                _comment = "Test approval comment";

                SetupMocks();

                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ILogger>());

                _result = _service.ApproveTemplate(_author, _templateId, _comment).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);
                _templateVersionBeforeUpdate = new TemplateVersion
                {
                    Name = "Original Test Name",
                    Description = "Original Description",
                    TemplateId = _templateId,
                    Version = 46,
                    MinorVersion = 16,
                    MajorVersion = 2,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateBeforeUpdate = new Template
                {
                    Name = _templateVersionBeforeUpdate.Name,
                    TemplateId = _templateVersionBeforeUpdate.TemplateId,
                    Current = _templateVersionBeforeUpdate
                };
                _templateRepository.GetTemplate(Arg.Is(_templateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.GetTemplateVersion(Arg.Is(_templateId), Arg.Is(_templateBeforeUpdate.Current.Version))
                    .Returns(_templateVersionBeforeUpdate);
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
            public void Saved_template_with_a_new_current_version()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Id != _templateVersionBeforeUpdate.Id));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_name()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Name == _templateVersionBeforeUpdate.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_description()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Description == _templateVersionBeforeUpdate.Description));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Author.Name == _author.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Version == _templateVersionBeforeUpdate.Version + 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_minor_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.MinorVersion == 0));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_major_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.MajorVersion == _templateVersionBeforeUpdate.MajorVersion + 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_publish_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.PublishStatus == PublishStatus.Draft));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Status == TemplateStatus.Published));
            }

            [TestMethod]
            public void Saved_version_with_correct_name()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Name == _templateVersionBeforeUpdate.Name));
            }

            [TestMethod]
            public void Saved_version_with_correct_description()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Description == _templateVersionBeforeUpdate.Description));
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateId == _templateBeforeUpdate.TemplateId));
            }

            [TestMethod]
            public void Saved_version_with_correct_status()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Status == TemplateStatus.Published));
            }

            [TestMethod]
            public void Saved_version_with_correct_version_number()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Version == _templateVersionBeforeUpdate.Version + 1));
            }

            [TestMethod]
            public void Saved_version_with_correct_minor_version_number()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.MinorVersion == 0));
            }

            [TestMethod]
            public void Saved_version_with_correct_major_version_number()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.MajorVersion == _templateVersionBeforeUpdate.MajorVersion + 1));
            }

            [TestMethod]
            public void Saved_version_with_correct_comment()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Comment == _comment));
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateJson()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateJson == _templateVersionBeforeUpdate.TemplateJson));
            }

            [TestMethod]
            public void Saved_version_with_recent_date()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Date > DateTimeOffset.Now.AddMinutes(-1)));
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingPeriodId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.FundingPeriodId == _templateVersionBeforeUpdate.FundingPeriodId));
            }
        }
        
        [TestClass]
        public class When_i_approve_previous_version_of_template
        {
            TemplateBuilderService _service;
            Reference _author;
            string _templateId;
            string _comment;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionPrevious;
            private TemplateVersion _templateVersionCurrent;

            public When_i_approve_previous_version_of_template()
            {
                _templateId = Guid.NewGuid().ToString();
                _author = new Reference("222", "SecondTestUser");
                _comment = "Test approval comment";

                SetupMocks();

                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ILogger>());

                _result = _service.ApproveTemplate(_author, _templateId, _comment, _templateVersionPrevious.Version.ToString()).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);
                _templateVersionPrevious = new TemplateVersion
                {
                    Name = "Previous Version Test Name",
                    Description = "Previous Version Description",
                    TemplateId = _templateId,
                    Version = 32,
                    MinorVersion = 2,
                    MajorVersion = 1,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateVersionCurrent = new TemplateVersion
                {
                    Name = "Current Version Test Name",
                    Description = "Current Version Description",
                    TemplateId = _templateId,
                    Version = 46,
                    MinorVersion = 16,
                    MajorVersion = 2,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateBeforeUpdate = new Template
                {
                    Name = _templateVersionPrevious.Name,
                    TemplateId = _templateVersionPrevious.TemplateId,
                    Current = _templateVersionCurrent
                };
                _templateRepository.GetTemplate(Arg.Is(_templateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.GetTemplateVersion(Arg.Is(_templateId), Arg.Is(_templateVersionPrevious.Version))
                    .Returns(_templateVersionPrevious);
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
            public void Saved_template_with_a_new_current_version()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => 
                    x.Current.Id != _templateVersionPrevious.Id && x.Current.Id != _templateVersionCurrent.Id));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_name()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Name == _templateVersionPrevious.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_description()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Description == _templateVersionPrevious.Description));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Author.Name == _author.Name));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Version == _templateVersionCurrent.Version + 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_minor_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.MinorVersion == 0));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_major_version_number()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.MajorVersion == _templateVersionCurrent.MajorVersion + 1));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_publish_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.PublishStatus == PublishStatus.Draft));
            }

            [TestMethod]
            public void Saved_current_version_with_correct_status()
            {
                _templateRepository.Received(1).Update(Arg.Is<Template>(x => x.Current.Status == TemplateStatus.Published));
            }

            [TestMethod]
            public void Saved_version_with_correct_name()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Name == _templateVersionPrevious.Name));
            }

            [TestMethod]
            public void Saved_version_with_correct_description()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Description == _templateVersionPrevious.Description));
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateId == _templateBeforeUpdate.TemplateId));
            }

            [TestMethod]
            public void Saved_version_with_correct_status()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Status == TemplateStatus.Published));
            }

            [TestMethod]
            public void Saved_version_with_correct_version_number()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Version == _templateVersionCurrent.Version + 1));
            }

            [TestMethod]
            public void Saved_version_with_correct_minor_version_number()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.MinorVersion == 0));
            }

            [TestMethod]
            public void Saved_version_with_correct_major_version_number()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.MajorVersion == _templateVersionCurrent.MajorVersion + 1));
            }

            [TestMethod]
            public void Saved_version_with_correct_comment()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Comment == _comment));
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateJson()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.TemplateJson == _templateVersionPrevious.TemplateJson));
            }

            [TestMethod]
            public void Saved_version_with_recent_date()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Date > DateTimeOffset.Now.AddMinutes(-1)));
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingPeriodId()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.FundingPeriodId == _templateVersionPrevious.FundingPeriodId));
            }
        }
    }
}