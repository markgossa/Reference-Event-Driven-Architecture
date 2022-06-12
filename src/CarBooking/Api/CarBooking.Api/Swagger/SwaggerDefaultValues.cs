using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LSE.Stocks.Api.Swagger;

public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        if (operation.Parameters == null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            if (parameter.Description is null)
            {
                parameter.Description = description.ModelMetadata?.Description;
            }

            if (parameter.Schema.Default is null && description.DefaultValue is not null)
            {
                parameter.Schema.Default = new OpenApiString(description.DefaultValue.ToString());
            }

            parameter.Required |= description.IsRequired;
        }
    }
}