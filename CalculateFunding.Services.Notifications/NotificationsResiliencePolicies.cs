using CalculateFunding.Services.Notifications.Interfaces;
using Polly;

namespace CalculateFunding.Services.Notifications
{
    public class NotificationsResiliencePolicies : INotificationsResiliencePolicies
    {
        public AsyncPolicy MessagePolicy { get; set; }
    }
}
