using System.Collections.Generic;
using CalculateFunding.Api.External.Swagger.Controller.Operations;
using CalculateFunding.Api.External.Swagger.Controller.Operations.Abstractions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger
{
    public class OperationFilterFactory : IOperationFilter
    {
        private IReadOnlyCollection<ISwaggerOperation> _swaggerOperations;

        public void InitializeOperations()
        {
            IReadOnlyCollection<ISwaggerOperation> swaggerOperations = new List<ISwaggerOperation>
            {
                new CommonResponseHeaders()
            };

            _swaggerOperations = swaggerOperations;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            InitializeOperations();
            foreach (ISwaggerOperation swaggerOperation in _swaggerOperations)
            {
                swaggerOperation.Apply(operation, context);
            }
        }

        //public void Apply(Operation operation, OperationFilterContext context)
        //{
        //    if (operation.OperationId == "Funding-streamsGet")
        //    {
        //        const string etagDescription = "An ETag of the resource";
        //        const string cacheControlDescription = "Caching information for the resource";
        //        const string lastModifiedDescription = "Date the resource was last modified";


        //        const string acceptHeaderDescription
        //            = "The calculate funding service uses the Media Type provided in the Accept header to determine " +
        //              "what representation of a particular resources to serve. " +
        //              "In particular this includes the version of the resource and the wire format.";
        //        const string ifNoneMatchDescription
        //            = "if a previously provided ETag value is provided, the service will return a 304 Not Modified response " +
        //              "is the resource has not changed.";

        //        operation.Parameters.Add(GenerateHeaderParameter("Accept", true, "string", acceptHeaderDescription));
        //        operation.Parameters.Add(GenerateHeaderParameter("If-None-Match", false, "string", ifNoneMatchDescription));

        //        operation.Responses.TryGetValue("200", out var response);
        //        if (response != null)
        //        {
        //            var headers = response.Headers ?? new Dictionary<string, Header>();
        //            headers.Add("ETag", GenerateHeaderResponse(etagDescription, "string"));
        //            headers.Add("Cache-Control", GenerateHeaderResponse(cacheControlDescription, "string"));
        //            headers.Add("Last-Modified", GenerateHeaderResponse(lastModifiedDescription, "date"));
        //            response.Headers = headers;
        //        }
        //    }
        //}

        //public IParameter GenerateHeaderParameter(string name, bool required, string type, string description)
        //{
        //    IParameter parameterGenerated = new NonBodyParameter()
        //    {
        //        Name = name,
        //        Required = required,
        //        Type = type,
        //        Description = description,
        //        In = "Header"
        //    };
        //    return parameterGenerated;
        //}

        //public Header GenerateHeaderResponse(string description, string type)
        //{
        //    Header headerGenerated = new Header
        //    {
        //        Description = "An ETag of the resource",
        //        Type = type
        //    };
        //    return headerGenerated;
        //}
    }
}
