using System.Net.Http;
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
    public static class Datasets
    {
        [FunctionName("datasets-search")]
        public static Task<IActionResult> RunSearchDataDefinitions(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (IServiceScope scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetSearchService svc = scope.ServiceProvider.GetService<IDatasetSearchService>();

                return svc.SearchDatasets(req);
            }
        }

        [FunctionName("create-new-dataset")]
        public static Task<IActionResult> RunCreateDataset(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.CreateNewDataset(req);
            }
        }

        [FunctionName("dataset-version-update")]
        public static Task<IActionResult> RunDatasetVersionUpdate(
         [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.DatasetVersionUpdate(req);
            }
        }

        [FunctionName("validate-dataset")]
        public static Task<IActionResult> RunValidateDataset(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.ValidateDataset(req);
            }
        }

        [FunctionName("get-datasources-by-relationshipid")]
        public static Task<IActionResult> RunGetDataSourcesByRelationshipId(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionSpecificationRelationshipService svc = scope.ServiceProvider.GetService<IDefinitionSpecificationRelationshipService>();

                return svc.GetDataSourcesByRelationshipId(req);
            }
        }

        [FunctionName("assign-datasource-to-relationship")]
        public static Task<IActionResult> RunAssignDatasourceVersionToRelationship(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDefinitionSpecificationRelationshipService svc = scope.ServiceProvider.GetService<IDefinitionSpecificationRelationshipService>();

                return svc.AssignDatasourceVersionToRelationship(req);
            }
        }

        [FunctionName("download-dataset-file")]
        public static Task<IActionResult> RunDownloadDatasetFile(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.DownloadDatasetFile(req);
            }
        }

        [FunctionName("dataset-reindex")]
        public static Task<IActionResult> RunDatasetReindex(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (var scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.Reindex(req);
            }
        }

        [FunctionName("get-currentdatasetversion-by-datasetid")]
        public static Task<IActionResult> RunGetCurrentDatasetVersionByDatasetId(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (IServiceScope scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.GetCurrentDatasetVersionByDatasetId(req);
            }
        }

        [FunctionName("regenerate-providersourcedatasets")]
        public static Task<IActionResult> RunRegenerateProviderSourceDatasets(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            using (IServiceScope scope = IocConfig.Build().CreateHttpScope(req))
            {
                IDatasetService svc = scope.ServiceProvider.GetService<IDatasetService>();

                return svc.RegenerateProviderSourceDatasets(req);
            }
        }
    }
}
