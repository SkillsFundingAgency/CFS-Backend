using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Jobs;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Calcs
{
    [TestClass]
    public partial class JobManagementServiceTests
    {
        [TestMethod]
        public void ProcesJobCompletion_MessageIsNull_ArgumentNullExceptionThrown()
        {
            // Arrange
            JobManagementService jobManagementService = CreateJobManagementService();

            // Act
            Func<Task> action = async () => await jobManagementService.ProcessJobCompletion(null);

            // Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("message");
        }

        [TestMethod]
        public void ProcessJobCompletion_MessageBodyIsNull_ArgumentNullExceptionThrown()
        {
            // Arrange
            JobManagementService jobManagementService = CreateJobManagementService();

            Message message = new Message();
            
            // Act
            Func<Task> action = async () => await jobManagementService.ProcessJobCompletion(message);

            // Assert
            action
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("message body");
        }

        [TestMethod]
        public async Task ProcessJobCompletion_JobIsNotComplete_ThenNoActionTaken()
        {
            // Arrange
            JobManagementService jobManagementService = CreateJobManagementService();

            Message message = new Message();

            // Act
            await jobManagementService.ProcessJobCompletion(message);

            // Assert

        }

        private JobManagementService CreateJobManagementService()
        {
            return new JobManagementService();
        }
    }
}
