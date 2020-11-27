using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Services.UnitTests;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.ServiceBus;
using System;
using CalculateFunding.Tests.Common.Builders;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    public abstract class ServiceTestsBase
    {
        protected Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }

        protected JobViewModel NewJobViewModel(Action<JobViewModelBuilder> setUp = null)
        {
            JobViewModelBuilder jobViewModelBuilder = new JobViewModelBuilder();

            setUp?.Invoke(jobViewModelBuilder);

            return jobViewModelBuilder.Build();
        }

        protected SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        protected Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        protected PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        protected TemplateFundingLine NewTemplateFundingLine(Action<TemplateFundingLineBuilder> setUp = null)
        {
            TemplateFundingLineBuilder templateFundingLineBuilder = new TemplateFundingLineBuilder();

            setUp?.Invoke(templateFundingLineBuilder);

            return templateFundingLineBuilder.Build();
        }

        protected ProviderCalculationResult NewProviderCalculationResult(Action<ProviderCalculationResultBuilder> setUp = null)
        {
            ProviderCalculationResultBuilder providerCalculationResultBuilder = new ProviderCalculationResultBuilder();

            setUp?.Invoke(providerCalculationResultBuilder);

            return providerCalculationResultBuilder.Build();
        }

        protected CalculationResult NewCalculationResult(Action<CalculationResultBuilder> setUp = null)
        {
            CalculationResultBuilder calculationResultBuilder = new CalculationResultBuilder();

            setUp?.Invoke(calculationResultBuilder);

            return calculationResultBuilder.Build();
        }

        protected TemplateMetadataContents NewTemplateMetadataContents(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder templateMetadataContentsBuilder = new TemplateMetadataContentsBuilder();

            setUp?.Invoke(templateMetadataContentsBuilder);

            return templateMetadataContentsBuilder.Build();
        }

        protected TemplateMapping NewTemplateMapping(Action<TemplateMappingBuilder> setUp = null)
        {
            TemplateMappingBuilder templateMappingBuilder = new TemplateMappingBuilder();

            setUp?.Invoke(templateMappingBuilder);

            return templateMappingBuilder.Build();
        }

        protected GeneratedProviderResult NewGeneratedProviderResult(Action<GeneratedProviderResultBuilder> setUp = null)
        {
            GeneratedProviderResultBuilder generatedProviderResultBuilder = new GeneratedProviderResultBuilder();

            setUp?.Invoke(generatedProviderResultBuilder);

            return generatedProviderResultBuilder.Build();
        }

        protected PublishedProviderFundingStructureItem NewPublishedProviderFundingStructureItem(Action<PublishedProviderFundingStructureItemBuilder> setUp = null)
        {
            PublishedProviderFundingStructureItemBuilder publishedProviderFundingStructureItemBuilder = new PublishedProviderFundingStructureItemBuilder();

            setUp?.Invoke(publishedProviderFundingStructureItemBuilder);

            return publishedProviderFundingStructureItemBuilder.Build();
        }

        protected string NewRandomString() => new RandomString();
        protected bool NewRandomBoolean() => new RandomBoolean();
        protected int NewRandomNumber() => new RandomNumberBetween(int.MinValue, int.MaxValue);
        protected uint NewRandomUInt() => (uint)new RandomNumberBetween(0, int.MaxValue);
        protected DateTime NewRandomDateTime() => new RandomDateTime();
    }
}
