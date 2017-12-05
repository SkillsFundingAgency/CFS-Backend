using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Functions.Datasets
{
    //public static class Providers
    //{
    //    [FunctionName("providers")]
    //    public static async Task<IActionResult> Run(
    //        [HttpTrigger(AuthorizationLevel.Function, "post", "get")] HttpRequest req, TraceWriter log)
    //    {
    //        req.Query.TryGetValue("urn", out var urn);

    //        if (req.Method == "POST")
    //        {
    //            return await OnPost(req);
    //        }

    //        return await OnGet(urn.FirstOrDefault());

    //    }

    //    private static async Task<IActionResult> OnGet(string urn)
    //    {
    //        var repository = ServiceFactory.GetService<Repository<Provider>>(); ;
            
    //        if (urn != null)
    //        {
    //            var item = await repository.ReadAsync(urn);
    //            if (item == null) return new NotFoundResult();
    //            return new JsonResult(JsonConvert.SerializeObject(item));

    //        }

    //        var items = repository.Query().ToList();
    //        return new OkObjectResult(JsonConvert.SerializeObject(items));
            
    //    }

    //    private static async Task<IActionResult> OnPost(HttpRequest req)
    //    {
    //        var repository = ServiceFactory.GetService<Repository<Provider>>();
    //        var json = await req.ReadAsStringAsync();

    //        var item = JsonConvert.DeserializeObject<Provider>(json);

    //        if (item == null)

    //        {
    //            var items = JsonConvert.DeserializeObject<List<Provider>>(json);
    //            if (items == null)
    //            {
    //                return new BadRequestErrorMessageResult("Please ensure item is passed in the request body");
    //            }

    //            await repository.BulkCreateAsync(items, 50);
    //        }

    //        await repository.CreateAsync(item);

    //        return new AcceptedResult();
    //    }
    //}

}
