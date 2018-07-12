using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.Controller.Operations.Abstractions
{
    public interface ISwaggerOperation
    {
        void Apply(Operation operation, OperationFilterContext context);
    }
}
