using CalculateFunding.Common.Models;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class MessageExtensionTests
    {
        [TestMethod]
        public void GetMessageBodyStringFromMessage_GivenUnCompressedBody_ReturnsJson()
        {
            //Arrange
            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = "spec-1",
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy1" } }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            byte[] messageBytes = Encoding.UTF8.GetBytes(json);

            Message message = new Message(messageBytes);

            //Act
            string result = MessageExtensions.GetMessageBodyStringFromMessage(message);

            //Assert
            result
                .Should()
                .BeEquivalentTo(json);
        }

        [TestMethod]
        public void GetMessageBodyStringFromMessage_GivenCompressedBody_ReturnsJson()
        {
            //Arrange
            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = new Models.Specs.SpecificationVersionComparisonModel()
            {
                Id = "spec-1",
                Current = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name",
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy2" } }
                },
                Previous = new Models.Specs.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Policies = new[] { new Models.Specs.Policy { Id = "pol-id", Name = "policy1" } }
                }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            byte[] messageBytes = json.Compress();

            Message message = new Message(messageBytes);
            message.UserProperties.Add("compressed", true);

            //Act
            string result = MessageExtensions.GetMessageBodyStringFromMessage(message);

            //Assert
            result
                .Should()
                .BeEquivalentTo(json);
        }
    }
}
