using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Core.FeatureToggles
{
    [TestClass]
    public class FeaturesTests
    {
        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsProviderProfilingServiceEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("providerProfilingServiceDisabled", 
                input, 
                output, 
                _ => _.IsProviderProfilingServiceDisabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsPublishButtonEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("publishButtonEnabled", 
                input, 
                output, 
                _ => _.IsPublishButtonEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsPublishAndApproveFilterEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("publishAndApprovePageFiltersEnabled", 
                input, 
                output, 
                _ => _.IsPublishAndApprovePageFiltersEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsNewEditCalculationPageEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("newEditCalculationPageEnabled", 
                input, 
                output, 
                _ => _.IsNewEditCalculationPageEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsNewManageDataSourcesPageEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("newManageDataSourcesPageEnabled", 
                input, 
                output, 
                _ => _.IsNewManageDataSourcesPageEnabled());
            
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsNewProviderCalculationResultsIndexEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("newProviderCalculationResultsIndexEnabled", 
                input, 
                output, 
                _ => _.IsNewProviderCalculationResultsIndexEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsProviderInformationViewInViewFundingPageEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("providerInformationViewInViewFundingPageEnabled", 
                input, 
                output, 
                _ => _.IsProviderInformationViewInViewFundingPageEnabled());
            
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsDynamicBuildProjectEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("dynamicBuildProjectEnabled", 
                    input, 
                    output, 
                    _ => _.IsDynamicBuildProjectEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsSearchModeAllEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("searchModeAllEnabled", 
                    input, 
                    output, 
                    _ => _.IsSearchModeAllEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsUseFieldDefinitionIdsInSourceDatasetsEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("useFieldDefinitionIdsInSourceDatasetsEnabled", 
                    input, 
                    output, 
                    _ => _.IsUseFieldDefinitionIdsInSourceDatasetsEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsProcessDatasetDefinitionNameChangesEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("processDatasetDefinitionNameChangesEnabled", 
                    input, 
                    output, 
                    _ => _.IsProcessDatasetDefinitionNameChangesEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsProcessDatasetDefinitionFieldChangesEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("processDatasetDefinitionFieldChangesEnabled", 
                    input, 
                    output, 
                    _ => _.IsProcessDatasetDefinitionFieldChangesEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsExceptionMessagesEnabled_ReturnsAsExpected(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("exceptionMessagesEnabled", 
                    input, 
                    output, 
                    _ => _.IsExceptionMessagesEnabled());
        }

        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsDeletePublishedProviderForbidden(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("deletePublishedProviderForbidden", 
                    input, 
                    output, 
                    _ => _.IsDeletePublishedProviderForbidden());
        }
        
        [TestMethod]
        [DynamicData(nameof(FeatureToggleTestCases), DynamicDataSourceType.Method)]
        public void IsCosmosDynamicScalingEnabled(string input, bool output)
        {
            AssertThatToggleMatchesConfigurationValue("cosmosDynamicScalingEnabled", 
                input, 
                output, 
                _ => _.IsCosmosDynamicScalingEnabled());
        }

        private void AssertThatToggleMatchesConfigurationValue(string configurationKey,
                string configurationValue,
                bool expectedValue,
                Func<Features, bool> toggle)
        {
                IConfigurationSection config = Substitute.For<IConfigurationSection>();
                
                config[configurationKey]
                        .Returns(configurationValue);

                toggle(new Features(config))
                        .Should()
                        .Be(expectedValue);       
        }

        private static IEnumerable<object[]> FeatureToggleTestCases()
        {
            yield return new object[] { null, false };
            yield return new object[] { string.Empty, false };
            yield return new object[] { "    ", false };
            yield return new object[] { "not a bool", false };
            yield return new object[] { "false", false };
            yield return new object[] { "FALSE", false };
            yield return new object[] { "FaLSe", false };
            yield return new object[] { "true", true };
            yield return new object[] { "TRUE", true };
            yield return new object[] { "tRue", true };
        }
    }
}
