using CalculateFunding.Services.Notifications.Interfaces;
using Polly;

namespace CalculateFunding.Services.Notifications
{
    public class NotificationsResilliencePolicies : INotificationsResilliencePolicies
    {
        public Policy MessagePolicy { get; set; }
    }
}
