using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.Controller.Operations.Abstractions
{
    public interface ISwaggerOperationFilterApplier
    {
        void Apply(Operation operation, OperationFilterContext context);
    }
}
