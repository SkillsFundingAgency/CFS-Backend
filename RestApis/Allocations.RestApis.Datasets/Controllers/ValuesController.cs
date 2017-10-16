using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Allocations.Models.Framework;
using Allocations.Respository;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace Allocations.RestApis.Datasets.Controllers
{
    public class ProviderDatasetsController : ApiController
    {
        private Repository _repository = new Repository("datasets");
        // GET api/values
        [Route("models/{modelName}/datasets/")]
        public async Task<IEnumerable<object>> Get()
        {
            return new[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [Route("models/{modelName}/datasets/")]
        public async Task Post(string modelName, string datasetName, [FromBody]string json)
        {
            var datasetType = AllocationFactory.GetDatasetType(datasetName);
            var dataset = JsonConvert.DeserializeObject(json, datasetType);

            await _repository.UpsertAsync(dataset);

        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }

}
