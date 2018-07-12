using System.Linq;
using CalculateFunding.Api.External.Swagger.Controller.Operations.Abstractions;
using Microsoft.EntityFrameworkCore.Internal;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.Controller.Operations.Abstractions
{
    public abstract class OperationAbs : ISwaggerOperation
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
