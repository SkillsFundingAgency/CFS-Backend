using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.External.Controllers
{
    public class DummyController
    {
        ///<summary>
        /// Gets value for a given id
        /// </summary>
        /// <param name="id">Id of the value searched for</param>
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }
    }
}
