using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models
{
    public class AzureBearerToken
    {
        public string AccessToken { get; set; }

        public int ExpiryLength { get; set; }
    }
}
