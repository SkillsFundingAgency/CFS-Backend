using Polly;

namespace CalculateFunding.Services.Notifications.Interfaces
{
    public interface INotificationsResilliencePolicies
    {
        Policy MessagePolicy { get; set; }
    }
}
