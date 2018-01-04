using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Calculations
    {

        [FunctionName("calculations-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
        {
            var restMethods =
                new RestCommandMethods<Specification, CalculationSpecificationCommand, CalculationSpecification>("spec-events")
                {
                    GetEntityId = command => command.SpecificationId,
                    UpdateTarget = (specification, command) =>
                    {
                        var policy = specification?.GetPolicy(command.PolicyId);
                        if (policy != null)
                        {
                            var existing = policy.GetCalculation(command.Content.Id);
                            if (existing != null)
                            {
                                existing.Name = command.Content.Name;
                                existing.Description = command.Content.Description;
                            }
                            else
                            {
                                policy.Calculations = policy.Calculations ?? new List<CalculationSpecification>();
                                policy.Calculations.Add(command.Content);
                            }
                        }


                        return specification;
                    }
                };
            return await restMethods.Run(req, log);
        }

    }

}
