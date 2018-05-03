using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    public class PreconditionFailedResult : ObjectResult
    {
        public PreconditionFailedResult(string message)
            :base(message)
        {
            this.StatusCode = 412;
        }
    }
}
