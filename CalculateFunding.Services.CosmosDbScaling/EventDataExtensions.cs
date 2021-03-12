using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public static class EventDataExtensions
    {
        public static TType? GetSystemProperty<TType>(this EventData eventData, string propertyName) where TType : struct
        {
            if (eventData.SystemProperties == null || eventData.SystemProperties.Count == 0 || !eventData.SystemProperties.TryGetValue(propertyName, out object property))
            {
                return null;
            }

            return (TType)property;
        }
    }
}
