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
    public class UpdateTemplateUnitTests
    {
        [TestClass]
        public class When_i_update_template_description
        {
            TemplateDescriptionUpdateCommand _command;
            TemplateBuilderService _service;
            Reference _originalAuthor;
            Reference _editor;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionFirst;
            private IPolicyRepository _policyRepository;
            private ISearchRepository<TemplateIndex> _searchRepository;
            private Template _savedTemplate;
            private TemplateVersion _savedTemplateVersion;

            public When_i_update_template_description()
            {
                _command = new TemplateDescriptionUpdateCommand
                {
                    Description = "Lorem ipsum",
                    TemplateId = Guid.NewGuid().ToString()
                };
                _originalAuthor = new Reference("111", "FirstTestUser");
                _editor = new Reference("222", "SecondTestUser");

                SetupMocks();
                
                _service = new TemplateBuilderService(
                    _validatorFactory,
                    Substitute.For<IFundingTemplateValidationService>(),
                    Substitute.For<ITemplateMetadataResolver>(),
                    _versionRepository,
                    _templateRepository,
                    _searchRepository,
                    _policyRepository,
                    Substitute.For<ITemplateBlobService>(),
                    Substitute.For<ILogger>());
                
                _result = _service.UpdateTemplateDescription(_command, _editor).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateBeforeUpdate = new Template
                {
                    Name = "Template Name",
                    TemplateId = _command.TemplateId,
                    Description = "Old Description",
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
                    }
                };
                
                _templateVersionFirst = new TemplateVersion
                {
                    Name = _templateBeforeUpdate.Name,
                    TemplateId = _command.TemplateId,
                    TemplateJson = @"{""$schema"":""https://fundingschemas.blob.core.windows.net/schemas/funding-template-schema-1.1.json"",""schemaVersion"":""1.1"",""fundingTemplate"":{""fundingLines"":[{""templateLineId"":1,""type"":""Payment"",""name"":""Funding Line 1"",""fundingLineCode"":""DSG-001"",""fundingLines"":[],""calculations"":[]}],""fundingPeriod"":{""id"":""XX-2021"",""period"":""2021"",""name"":""XX-2021"",""type"":""FY"",""startDate"":""2020-04-01T00:00:00+00:00"",""endDate"":""2021-03-31T00:00:00+00:00""},""fundingStream"":{""code"":""DSG"",""name"":""DSG""},""fundingTemplateVersion"":""0.1""}}",
                    Version = 1,
                    MinorVersion = 1,
                    MajorVersion = 0,
                    FundingPeriodId = _templateBeforeUpdate.FundingPeriod.Id,
                    FundingStreamId = _templateBeforeUpdate.FundingStream.Id,
                    SchemaVersion = "1.1",
                    Status = TemplateStatus.Published,
                    Author = _originalAuthor
                };
                _templateBeforeUpdate.Current = _templateVersionFirst;
                
                _templateRepository.GetTemplate(Arg.Is(_command.TemplateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Do<Template>(x => _savedTemplate = x)).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.SaveVersion(Arg.Do<TemplateVersion>(x => _savedTemplateVersion = x)).Returns(HttpStatusCode.OK);

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
            public void Saved_template_with_a_current_version()
            {
                _savedTemplate?.Current.Should().NotBeNull();
            }

            [TestMethod]
            public void Saved_current_version_with_correct_description()
            {
                _savedTemplate?.Description.Should().Be(_command.Description);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_name()
            {
                _savedTemplate?.Current?.Name.Should().Be(_templateBeforeUpdate.Name);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _savedTemplate?.Current?.Author?.Name.Should().Be(_originalAuthor.Name);
                _savedTemplate?.Current?.Author?.Id.Should().Be(_originalAuthor.Id);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _savedTemplate?.Current?.Version.Should().Be(_templateVersionFirst.Version);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_minor_version_number()
            {
                _savedTemplate?.Current?.MinorVersion.Should().Be(_templateVersionFirst.MinorVersion);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_major_version_number()
            {
                _savedTemplate?.Current?.MajorVersion.Should().Be(_templateVersionFirst.MajorVersion);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_publish_status()
            {
                _savedTemplate?.Current?.PublishStatus.Should().Be(PublishStatus.Draft);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_status()
            {
                _savedTemplate?.Current?.Status.Should().Be(TemplateStatus.Published);
            }

            [TestMethod]
            public void Saved_version_with_correct_name()
            {
                _savedTemplateVersion?.Name.Should().Be(_templateBeforeUpdate.Name);
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateId()
            {
                _savedTemplateVersion?.TemplateId.Should().Be(_templateBeforeUpdate.TemplateId);
            }

            [TestMethod]
            public void Saved_version_with_correct_status()
            {
                _savedTemplateVersion?.Status.Should().Be(TemplateStatus.Draft);
            }

            [TestMethod]
            public void Saved_version_with_blank_comment()
            {
                _savedTemplateVersion?.Comment.Should().BeNullOrEmpty();
            }

            [TestMethod]
            public void Saved_version_with_recent_date()
            {
                _savedTemplateVersion?.Date.Should().BeAfter(DateTimeOffset.Now.AddMinutes(-1));
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingPeriodId()
            {
                _savedTemplateVersion?.FundingPeriodId.Should().Be(_templateVersionFirst.FundingPeriodId);
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingStreamId()
            {
                _savedTemplateVersion?.FundingStreamId.Should().Be(_templateVersionFirst.FundingStreamId);
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateJson()
            {
                _savedTemplateVersion?.TemplateJson.Should().Be(_templateVersionFirst.TemplateJson);
            }

            [TestMethod]
            public void Saved_version_without_copying_across_comment()
            {
                _savedTemplateVersion?.Comment.Should().BeNullOrEmpty();
            }
        }
        
        [TestClass]
        public class When_i_update_template_content
        {
            TemplateFundingLinesUpdateCommand _command;
            TemplateBuilderService _service;
            Reference _author;
            CommandResult _result;
            private ITemplateVersionRepository _versionRepository;
            private ITemplateRepository _templateRepository;
            private IIoCValidatorFactory _validatorFactory;
            private Template _templateBeforeUpdate;
            private TemplateVersion _templateVersionFirst;
            private IFundingTemplateValidationService _templateValidationService;
            private ITemplateMetadataGenerator _templateMetadataGenerator;
            private ITemplateMetadataResolver _templateMetadataResolver;
            private IPolicyRepository _policyRepository;
            private ISearchRepository<TemplateIndex> _searchRepository;
            private Template _savedTemplate;
            private TemplateVersion _savedTemplateVersion;
            private ITemplateBlobService _templateBlobService;

            public When_i_update_template_content()
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
                
                _result = _service.UpdateTemplateContent(_command, _author).GetAwaiter().GetResult();
            }

            private void SetupMocks()
            {
                _validatorFactory = Substitute.For<IIoCValidatorFactory>();
                _validatorFactory.Validate(Arg.Any<object>()).Returns(new ValidationResult());
                _templateRepository = Substitute.For<ITemplateRepository>();
                _templateVersionFirst = new TemplateVersion
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
                _templateBeforeUpdate = new Template
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
                    Current = _templateVersionFirst
                };

                _templateMetadataGenerator = Substitute.For<ITemplateMetadataGenerator>();
                _templateMetadataGenerator.Validate(Arg.Any<string>()).Returns(new ValidationResult());
                _templateMetadataResolver = Substitute.For<ITemplateMetadataResolver>();
                _templateMetadataResolver.GetService(Arg.Any<string>()).Returns(_templateMetadataGenerator);
                _templateValidationService = Substitute.For<IFundingTemplateValidationService>();
                _templateValidationService.ValidateFundingTemplate(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), null).Returns(new FundingTemplateValidationResult { });
                _templateRepository.GetTemplate(Arg.Is(_command.TemplateId)).Returns(_templateBeforeUpdate);
                _templateRepository.Update(Arg.Do<Template>(x => _savedTemplate = x)).Returns(HttpStatusCode.OK);

                _versionRepository = Substitute.For<ITemplateVersionRepository>();
                _versionRepository.SaveVersion(Arg.Do<TemplateVersion>(x => _savedTemplateVersion = x)).Returns(HttpStatusCode.OK);

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
                _result.Version.Should().Be(_templateVersionFirst.Version + 1);
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
            public void Saved_template_with_a_current_version()
            {
                _savedTemplate?.Current.Should().NotBeNull();
            }

            [TestMethod]
            public void Saved_current_version_with_correct_json()
            {
                _savedTemplate?.Current?.TemplateJson.Should().Contain(_command.TemplateFundingLinesJson);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_name()
            {
                _savedTemplate?.Current?.Name.Should().Be(_templateBeforeUpdate.Name);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_author()
            {
                _savedTemplate?.Current?.Author?.Name.Should().Be(_author.Name);
                _savedTemplate?.Current?.Author?.Id.Should().Be(_author.Id);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_version_number()
            {
                _savedTemplate?.Current?.Version.Should().Be(_templateVersionFirst.Version + 1);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_minor_version_number()
            {
                _savedTemplate?.Current?.MinorVersion.Should().Be(_templateVersionFirst.MinorVersion + 1);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_major_version_number()
            {
                _savedTemplate?.Current?.MajorVersion.Should().Be(_templateVersionFirst.MajorVersion);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_publish_status()
            {
                _savedTemplate?.Current?.PublishStatus.Should().Be(PublishStatus.Draft);
            }

            [TestMethod]
            public void Saved_current_version_with_correct_status()
            {
                _savedTemplate?.Current?.Status.Should().Be(TemplateStatus.Draft);
            }

            [TestMethod]
            public void Saved_version_with_correct_name()
            {
                _savedTemplateVersion?.Name.Should().Be(_templateBeforeUpdate.Name);
            }

            [TestMethod]
            public void Saved_version_with_correct_TemplateId()
            {
                _savedTemplateVersion?.TemplateId.Should().Be(_templateBeforeUpdate.TemplateId);
            }

            [TestMethod]
            public void Saved_version_with_correct_status()
            {
                _savedTemplateVersion?.Status.Should().Be(TemplateStatus.Draft);
            }

            [TestMethod]
            public void Saved_version_with_blank_comment()
            {
                _savedTemplateVersion?.Comment.Should().BeNullOrEmpty();
            }

            [TestMethod]
            public void Saved_version_with_recent_date()
            {
                _savedTemplateVersion?.Date.Should().BeAfter(DateTimeOffset.Now.AddMinutes(-1));
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingPeriodId()
            {
                _savedTemplateVersion?.FundingPeriodId.Should().Be(_templateVersionFirst.FundingPeriodId);
            }

            [TestMethod]
            public void Saved_version_with_correct_FundingStreamId()
            {
                _savedTemplateVersion?.FundingStreamId.Should().Be(_templateVersionFirst.FundingStreamId);
            }
        }
    }
}