using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Options
{
    public class ServiceBusSettings
    {
        public string ServiceBusConnectionString { get; set; }

        public string SpecsServiceBusTopicName { get; set; }

        public string CalcsServiceBusTopicName { get; set; }
    }
}
