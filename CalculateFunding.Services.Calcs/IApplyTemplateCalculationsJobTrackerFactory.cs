using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs
{
    public interface IApplyTemplateCalculationsJobTrackerFactory
    {
        IApplyTemplateCalculationsJobTracker CreateJobTracker(Message message);
    }
}