using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.FeatureToggles.UnitTests
{
    [TestClass]
    public class FeaturesTests
    {
        [TestMethod]
        public void IsAllocationLineMajorMinorVersioningEnabled_WhenConfigNotPresent_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("allocationLineMajorMinorVersioningEnabled")].Returns((string)null);

            Features features = new Features(config);

            // Act
            bool result = features.IsAllocationLineMajorMinorVersioningEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAllocationLineMajorMinorVersioningEnabled_WhenConfigEmptyString_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("allocationLineMajorMinorVersioningEnabled")].Returns(string.Empty);

            Features features = new Features(config);

            // Act
            bool result = features.IsAllocationLineMajorMinorVersioningEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAllocationLineMajorMinorVersioningEnabled_WhenConfigNotBoolean_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("allocationLineMajorMinorVersioningEnabled")].Returns("not a bool");

            Features features = new Features(config);

            // Act
            bool result = features.IsAllocationLineMajorMinorVersioningEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAllocationLineMajorMinorVersioningEnabled_WhenConfigFalse_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("allocationLineMajorMinorVersioningEnabled")].Returns("false");

            Features features = new Features(config);

            // Act
            bool result = features.IsAllocationLineMajorMinorVersioningEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAllocationLineMajorMinorVersioningEnabled_WhenConfigTrue_ReturnsTrue()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("allocationLineMajorMinorVersioningEnabled")].Returns("true");

            Features features = new Features(config);

            // Act
            bool result = features.IsAllocationLineMajorMinorVersioningEnabled();

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsProviderProfilingServiceEnabled_WhenConfigNotPresent_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("providerProfilingServiceDisabled")].Returns((string)null);

            Features features = new Features(config);

            // Act
            bool result = features.IsProviderProfilingServiceDisabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsProviderProfilingServiceEnabled_WhenConfigEmptyString_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("providerProfilingServiceDisabled")].Returns(string.Empty);

            Features features = new Features(config);

            // Act
            bool result = features.IsProviderProfilingServiceDisabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsProviderProfilingServiceEnabled_WhenConfigNotBoolean_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("providerProfilingServiceDisabled")].Returns("not a bool");

            Features features = new Features(config);

            // Act
            bool result = features.IsProviderProfilingServiceDisabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsProviderProfilingServiceEnabled_WhenConfigFalse_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("providerProfilingServiceDisabled")].Returns("false");

            Features features = new Features(config);

            // Act
            bool result = features.IsProviderProfilingServiceDisabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsProviderProfilingServiceEnabled_WhenConfigTrue_ReturnsTrue()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("providerProfilingServiceDisabled")].Returns("true");

            Features features = new Features(config);

            // Act
            bool result = features.IsProviderProfilingServiceDisabled();

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsAggregateSupportInCalculationsEnabled_WhenConfigNotPresent_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("aggregateSupportInCalculationsEnabled")].Returns((string)null);

            Features features = new Features(config);

            // Act
            bool result = features.IsAggregateSupportInCalculationsEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAggregateSupportInCalculationsEnabled_WhenConfigEmptyString_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("aggregateSupportInCalculationsEnabled")].Returns(string.Empty);

            Features features = new Features(config);

            // Act
            bool result = features.IsAggregateSupportInCalculationsEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAggregateSupportInCalculationsEnabled_WhenConfigNotBoolean_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("aggregateSupportInCalculationsEnabled")].Returns("not a bool");

            Features features = new Features(config);

            // Act
            bool result = features.IsAggregateSupportInCalculationsEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAggregateSupportInCalculationsEnabled_WhenConfigFalse_ReturnsFalse()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("aggregateSupportInCalculationsEnabled")].Returns("false");

            Features features = new Features(config);

            // Act
            bool result = features.IsAggregateSupportInCalculationsEnabled();

            // Assert
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsAggregateSupportInCalculationsEnabled_WhenConfigTrue_ReturnsTrue()
        {
            // Arrange
            IConfigurationSection config = Substitute.For<IConfigurationSection>();
            config[Arg.Is("aggregateSupportInCalculationsEnabled")].Returns("true");

            Features features = new Features(config);

            // Act
            bool result = features.IsAggregateSupportInCalculationsEnabled();

            // Assert
            result.Should().BeTrue();
        }
    }
}
