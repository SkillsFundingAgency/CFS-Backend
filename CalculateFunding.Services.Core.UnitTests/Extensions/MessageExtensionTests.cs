using CalculateFunding.Common.Models;
using CalculateFunding.Models.Messages;
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
            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = "spec-1",
                Current = new Models.Messages.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" },
                    Name = "any-name"
                },
                Previous = new Models.Messages.SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp1" }
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
    }
}
