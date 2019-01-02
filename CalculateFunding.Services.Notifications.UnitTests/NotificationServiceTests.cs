using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using NSubstitute;

namespace CalculateFunding.Services.Notifications.UnitTests
{
    public partial class NotificationServiceTests
    {
        public const string JobId = "job1";

        public const string SpecificationId = "7BE80DFD-F1E6-483C-9B03-16DFE3CBBE37";

        public const string ParentJobId = "parentJobId";


        protected NotificationService CreateService()
        {
            return new NotificationService();
        }

        protected IAsyncCollector<SignalRMessage> CreateSignalRMessageCollector()
        {
            return Substitute.For<IAsyncCollector<SignalRMessage>>();
        }
    }
}
