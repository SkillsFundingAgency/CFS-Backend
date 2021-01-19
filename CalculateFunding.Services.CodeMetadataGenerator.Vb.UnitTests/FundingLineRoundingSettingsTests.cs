using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.CodeMetadataGenerator.Vb.UnitTests
{
    [TestClass]
    public class FundingLineRoundingSettingsTests
    {
        private Mock<IConfiguration> _configuration;

        private FundingLineRoundingSettings _settings;

        [TestInitialize]
        public void SetUp()
        {
            _configuration = new Mock<IConfiguration>();

            _settings = new FundingLineRoundingSettings(_configuration.Object);
        }

        [TestMethod]
        public void DefaultsTo2WhenNothingConfiguredForDecimalPlaces()
        {
            _settings
                .DecimalPlaces
                .Should()
                .Be(2);
        }

        [TestMethod]
        public void ReadsDecimalPlacesSettingFromConfigurationSettings()
        {
            int expectedDecimalPlaces = NewRandomNumber();
            
            GivenTheConfigurationSetting("FundingLineRoundingSettings:DecimalPlaces", expectedDecimalPlaces.ToString());

            _settings
                .DecimalPlaces
                .Should()
                .Be(expectedDecimalPlaces);
        }

        private static RandomNumberBetween NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);

        private void GivenTheConfigurationSetting(string key,
            string value)
            => _configuration.Setup(_ => _[key])
                .Returns(value);
    }
}