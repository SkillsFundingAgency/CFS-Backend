using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CalculateFunding.Functions.Datasets.Http
{
    public static class DataDefinitions
    {
        [FunctionName("data-definitions")]
        public static Task<IActionResult> RunDataDefinitions(
             [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionsService svc = scope.ServiceProvider.GetService<IDefinitionsService>();

                return svc.SaveDefinition(req);
            }
        }

        [FunctionName("get-data-definitions")]
        public static Task<IActionResult> RunGetDataDefinitions(
         [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionsService svc = scope.ServiceProvider.GetService<IDefinitionsService>();

                return svc.GetDatasetDefinitions(req);
            }
        }

        [FunctionName("create-definitionspecification-relationship")]
        public static Task<IActionResult> RunCreateDefinitionSpecificationRelationship(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionSpecificationRelationshipService svc = scope.ServiceProvider.GetService<IDefinitionSpecificationRelationshipService>();

                return svc.CreateRelationship(req);
            }
        }

        [FunctionName("get-definitions-relationships")]
        public static Task<IActionResult> RunGetDefinitionRelationships(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionSpecificationRelationshipService svc = scope.ServiceProvider.GetService<IDefinitionSpecificationRelationshipService>();

                return svc.GetRelationshipsBySpecificationId(req);
            }
        }

        [FunctionName("get-definition-relationship-by-specificationid-name")]
        public static Task<IActionResult> RunGetDefinitionRelationshipBySpecificationIdAndName(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionSpecificationRelationshipService svc = scope.ServiceProvider.GetService<IDefinitionSpecificationRelationshipService>();

                return svc.GetRelationshipBySpecificationIdAndName(req);
            }
        }

        [FunctionName("get-relationships-by-specificationId")]
        public static Task<IActionResult> RunGetRealtionshipsBySpecificationId(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionSpecificationRelationshipService svc = scope.ServiceProvider.GetService<IDefinitionSpecificationRelationshipService>();

                return svc.GetCurrentRelationshipsBySpecificationId(req);
            }
        }
    }
}
