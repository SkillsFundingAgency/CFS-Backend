using Polly;

namespace CalculateFunding.Services.Notifications.Interfaces
{
    public interface INotificationsResiliencePolicies
    {
        AsyncPolicy MessagePolicy { get; set; }
    }
}
