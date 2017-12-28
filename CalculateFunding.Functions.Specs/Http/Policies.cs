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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Specs.Http
{
    public static class Policies
    {
        [FunctionName("policies-commands")]
        public static async Task<IActionResult> RunCommands(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, TraceWriter log)
        {
            var restMethods =
                new RestCommandMethods<Specification, PolicySpecificationCommand, PolicySpecification>
                {
                    GetEntityId = command => command.SpecificationId,
                    UpdateTarget = (specification, command) =>
                    {
                        var existing = specification?.GetPolicy(command.Id);
                        if (existing != null)
                        {
                            existing.Name = command.Content.Name;
                            existing.Description = command.Content.Description;
                        }
                        var parent = specification?.GetPolicy(command.ParentPolicyId);
                        if (parent != null)
                        {
                            parent.SubPolicies = parent.SubPolicies ?? new List<PolicySpecification>();
                            parent.SubPolicies.Add(command.Content);
                        }
                        else
                        {
                            specification.Policies = specification.Policies ?? new List<PolicySpecification>();
                            specification.Policies.Add(command.Content);
                        }

                        return specification;
                    }
                };
            return await restMethods.Run(req, log);
        }
    }

}
