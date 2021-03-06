﻿using System;
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
    public class ApproveTemplateUnitTests
    {
        [TestClass]
        public class When_i_publish_current_template
        {
            TemplateBuilderService _service;
            Reference _author;
            string _templateId;
            string _publishNote;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionBeforeUpdate;
            private ITemplateBlobService _templateBlobService;

            public When_i_publish_current_template()
            {
                _templateId = Guid.NewGuid().ToString();
                _author = new Reference("222", "SecondTestUser");
                _publishNote = "Test publish note";

                SetupMocks();

                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ISearchRepository<TemplateIndex>>(),
                    Substitute.For<IPolicyRepository>(),
                    _templateBlobService,
                    Substitute.For<ILogger>());

                TemplatePublishCommand command = new TemplatePublishCommand
                {
                    Author = _author,
                    TemplateId = _templateId,
                    Note = _publishNote
                };
                _result = _service.PublishTemplate(command).GetAwaiter().GetResult();
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
                    TemplateId = _templateId,
                    Version = 46,
                    MinorVersion = 16,
                    MajorVersion = 2,
                    TemplateJson = "{ \"Lorem\": \"ipsum\" }",
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateBeforeUpdate = new Template
                {
                    Name = _templateVersionBeforeUpdate.Name,
                    TemplateId = _templateVersionBeforeUpdate.TemplateId,
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
                    Current = _templateVersionBeforeUpdate
                };
                _templateRepository.GetTemplate(Arg.Is(_templateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.GetTemplateVersion(Arg.Is(_templateId), Arg.Is(_templateBeforeUpdate.Current.Version))
                    .Returns(_templateVersionBeforeUpdate);
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);
                _templateBlobService = Substitute.For<ITemplateBlobService>();
                _templateBlobService.PublishTemplate(Arg.Any<Template>()).Returns(CommandResult.Success());
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
                    .BeTrue(
                        $"Unexpected validation errors: {_result.ValidationResult?.Errors.Select(x => x.ErrorMessage).Aggregate((x, y) => x + ", " + y)}");
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
                _templateRepository.Received(1)
                    .Update(Arg.Is<Template>(x => x.Current.MajorVersion == _templateVersionBeforeUpdate.MajorVersion + 1));
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
                _versionRepository.Received(1)
                    .SaveVersion(Arg.Is<TemplateVersion>(x => x.MajorVersion == _templateVersionBeforeUpdate.MajorVersion + 1));
            }

            [TestMethod]
            public void Saved_version_with_correct_note()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Comment == _publishNote));
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
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.FundingPeriodId == _templateBeforeUpdate.FundingPeriod.Id));
            }
        }

        [TestClass]
        public class When_i_try_to_publish_current_template_without_content
        {
            TemplateBuilderService _service;
            Reference _author;
            string _templateId;
            string _publishNote;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionBeforeUpdate;
            private ITemplateBlobService _templateBlobService;

            public When_i_try_to_publish_current_template_without_content()
            {
                _templateId = Guid.NewGuid().ToString();
                _author = new Reference("222", "SecondTestUser");
                _publishNote = "Test publish note";

                SetupMocks();

                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ISearchRepository<TemplateIndex>>(),
                    Substitute.For<IPolicyRepository>(),
                    _templateBlobService,
                    Substitute.For<ILogger>());

                TemplatePublishCommand command = new TemplatePublishCommand
                {
                    Author = _author,
                    TemplateId = _templateId,
                    Note = _publishNote
                };
                _result = _service.PublishTemplate(command).GetAwaiter().GetResult();
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
                    TemplateId = _templateId,
                    Version = 46,
                    TemplateJson = "",
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
                    Current = _templateVersionBeforeUpdate
                };
                _templateRepository.GetTemplate(Arg.Is(_templateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.GetTemplateVersion(Arg.Is(_templateId), Arg.Is(_templateBeforeUpdate.Current.Version))
                    .Returns(_templateVersionBeforeUpdate);
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);
                _templateBlobService = Substitute.For<ITemplateBlobService>();
                _templateBlobService.PublishTemplate(Arg.Any<Template>()).Returns(CommandResult.Success());
            }

            [TestMethod]
            public void Results_in_failure()
            {
                _result.Succeeded.Should().BeFalse();
                _result.Exception.Should().BeNull();
            }

            [TestMethod]
            public void Returns_validation_errors()
            {
                _result.ValidationResult.IsValid.Should().BeFalse();
            }
        }

        [TestClass]
        public class When_i_publish_previous_version_of_template
        {
            TemplateBuilderService _service;
            Reference _author;
            string _templateId;
            string _publishNote;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionPrevious;
            private TemplateVersion _templateVersionCurrent;
            private ITemplateBlobService _templateBlobService;

            public When_i_publish_previous_version_of_template()
            {
                _templateId = Guid.NewGuid().ToString();
                _author = new Reference("222", "SecondTestUser");
                _publishNote = "Test publish comment";

                SetupMocks();

                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    Substitute.For<ISearchRepository<TemplateIndex>>(),
                    Substitute.For<IPolicyRepository>(),
                    _templateBlobService,
                    Substitute.For<ILogger>());

                TemplatePublishCommand command = new TemplatePublishCommand
                {
                    Author = _author,
                    TemplateId = _templateId,
                    Version = _templateVersionPrevious.Version.ToString(),
                    Note = _publishNote
                };
                _result = _service.PublishTemplate(command).GetAwaiter().GetResult();
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
                    TemplateId = _templateId,
                    Version = 32,
                    MinorVersion = 2,
                    MajorVersion = 1,
                    TemplateJson = "{ \"Lorem\": \"ipsum\" }",
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateVersionCurrent = new TemplateVersion
                {
                    Name = "Current Version Test Name",
                    TemplateId = _templateId,
                    Version = 46,
                    MinorVersion = 16,
                    MajorVersion = 2,
                    TemplateJson = "{ \"Lorem\": \"ipsum\" }",
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Draft,
                    Author = new Reference("111", "FirstTestUser")
                };
                _templateBeforeUpdate = new Template
                {
                    Name = _templateVersionPrevious.Name,
                    TemplateId = _templateVersionPrevious.TemplateId,
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
                _templateRepository.GetTemplate(Arg.Is(_templateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Any<Template>()).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.GetTemplateVersion(Arg.Is(_templateId), Arg.Is(_templateVersionPrevious.Version))
                    .Returns(_templateVersionPrevious);
                _versionRepository.SaveVersion(Arg.Any<TemplateVersion>()).Returns(HttpStatusCode.OK);
                _templateBlobService = Substitute.For<ITemplateBlobService>();
                _templateBlobService.PublishTemplate(Arg.Any<Template>()).Returns(CommandResult.Success());
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
                    .BeTrue(
                        $"Unexpected validation errors: {_result.ValidationResult?.Errors.Select(x => x.ErrorMessage).Aggregate((x, y) => x + ", " + y)}");
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
            public void Saved_version_with_correct_note()
            {
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.Comment == _publishNote));
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
                _versionRepository.Received(1).SaveVersion(Arg.Is<TemplateVersion>(x => x.FundingPeriodId == _templateBeforeUpdate.FundingPeriod.Id));
            }
        }
    }
}