using System.Collections.Generic;
using CalculateFunding.Api.External.Swagger.Operations;
using CalculateFunding.Api.External.Swagger.Operations.Abstractions;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger
{
    public class OperationFilterFactory : IOperationFilter
    {
        private IReadOnlyCollection<ISwaggerOperationFilterApplier> _swaggerOperations;

        public void InitializeOperations()
        {
            IReadOnlyCollection<ISwaggerOperationFilterApplier> swaggerOperations = new List<ISwaggerOperationFilterApplier>
            {
                new CommonResponseHeadersFilterApplier()
            };

            _swaggerOperations = swaggerOperations;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            InitializeOperations();
            foreach (ISwaggerOperationFilterApplier swaggerOperation in _swaggerOperations)
            {
                swaggerOperation.Apply(operation, context);
            }
        }
    }
}