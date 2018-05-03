using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    public class InternalServerErrorResult : ObjectResult
    {
        public InternalServerErrorResult(string message)
            :base(message)
        {
            this.StatusCode = 500;
        }
    }
}
