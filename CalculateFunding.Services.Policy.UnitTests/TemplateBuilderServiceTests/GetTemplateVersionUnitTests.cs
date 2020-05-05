using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.TemplateMetadata;
using CalculateFunding.Models.Policy.TemplateBuilder;
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
    [TestClass]
    public class GetTemplateVersionUnitTests
    {
        private readonly string _templateId = Guid.NewGuid().ToString();

        [TestMethod]
        [DataRow(new TemplateStatus[0], 3)]
        [DataRow(new TemplateStatus[] {TemplateStatus.Updated}, 1)]
        [DataRow(new TemplateStatus[] {TemplateStatus.Approved, TemplateStatus.Updated}, 2)]
        public async Task ReturnsFilteredStatuses(TemplateStatus[] statusFilters, int expectedCount)
        {
            TemplateBuilderService sut = GetTemplateBuilderService();

            IEnumerable<TemplateVersionResponse> results = await sut.GetTemplateVersions(_templateId, statusFilters.ToList());

            List<TemplateVersionResponse> templateVersionResponses = results.ToList();
            templateVersionResponses.Should().HaveCount(expectedCount);
        }

        private TemplateBuilderService GetTemplateBuilderService()
        {
            ITemplateVersionRepository versionRepository = Substitute.For<ITemplateVersionRepository>();
            versionRepository.GetVersions(_templateId).Returns(new List<TemplateVersion>
            {
                new TemplateVersion
                {
                    Status = TemplateStatus.Approved
                },
                new TemplateVersion
                {
                    Status = TemplateStatus.Draft
                },
                new TemplateVersion
                {
                    Status = TemplateStatus.Updated
                }
            });

            TemplateBuilderService service = new TemplateBuilderService(
                Substitute.For<IIoCValidatorFactory>(),
                Substitute.For<IFundingTemplateValidationService>(),
                Substitute.For<ITemplateMetadataResolver>(),
                versionRepository,
                Substitute.For<ITemplateRepository>(),
                Substitute.For<ILogger>());

            return service;
        }
    }
}
