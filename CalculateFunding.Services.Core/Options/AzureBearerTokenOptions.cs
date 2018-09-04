using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Options
{
    public class AzureBearerTokenOptions
    {
        public string Url { get; set; }

        public string GrantType { get; set; }

        public string Scope { get; set; }

        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}
