using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class DatasetsDataCopyService : JobProcessingService, IDatasetsDataCopyService
    {
        public DatasetsDataCopyService(
            IJobManagement jobManagement,
            ILogger logger) : base(jobManagement, logger)
        {

        }

        public override Task Process(Message message)
        {
            return Task.CompletedTask;
        }
    }
}
