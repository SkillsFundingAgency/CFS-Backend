using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IGherkinParserService
    {
        Task<IActionResult> ValidateGherkin(HttpRequest request);
    }
}
