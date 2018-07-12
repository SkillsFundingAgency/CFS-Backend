using System.Linq;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.Controller.Operations.Abstractions
{
    public abstract class SwaggerOperationFilterApplierAbs : ISwaggerOperationFilterApplier
    {
        public abstract string[] GetOperationIds();

        public bool ShouldApplyForOperation(string operationId)
        {
            return GetOperationIds().Any(o => o == operationId);
        }
        public abstract void ApplySwaggerComponents(Operation operation, OperationFilterContext context);

        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (ShouldApplyForOperation(operation.OperationId))
            {
                ApplySwaggerComponents(operation, context);
            }
        }
    }
}
