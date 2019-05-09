using Polly;

namespace CalculateFunding.Services.Notifications.Interfaces
{
    public interface INotificationsResiliencePolicies
    {
        Policy MessagePolicy { get; set; }
    }
}
