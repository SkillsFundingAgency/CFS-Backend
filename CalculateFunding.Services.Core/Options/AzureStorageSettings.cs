using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Options
{
    public class AzureStorageSettings
    {
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
    }
}
