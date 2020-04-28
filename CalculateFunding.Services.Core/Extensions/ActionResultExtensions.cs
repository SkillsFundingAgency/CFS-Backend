using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class ActionResultExtensions
    {
        public static bool IsOk(this IActionResult actionResult )
        {
            return actionResult is OkObjectResult;
        }
    }
}
