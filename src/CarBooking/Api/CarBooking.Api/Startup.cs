using LSE.Stocks.Api.ApplicationBuilderExtensions;
using LSE.Stocks.Api.ServiceCollectionExtensions;
using LSE.Stocks.Api.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IO;
using System.Reflection;

namespace LSE.Stocks.Api;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => Configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer()
            .AddSwaggerGen(o => ConfigureSwaggerGenOptions(o))
            .AddMediatorServices()
            .AddApplicationInsightsTelemetry()
            .AddServices(Configuration)
            .AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerGenOptions>()
            .AddControllers();

        AddApiVersioning(services);
    }

    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env,
        IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(o =>
            {
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    o.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                        $"LSE.Stocks.API - {description.GroupName.ToUpper()}");
                }
            });
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseCustomExceptionHandlerMiddleware();
        app.AddCorrelationIdMiddleware();

        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }

    private static void ConfigureSwaggerGenOptions(SwaggerGenOptions o)
    {
        AddSwaggerXmlComments(o);
        o.OperationFilter<SwaggerDefaultValues>();
    }

    private static void AddSwaggerXmlComments(SwaggerGenOptions o)
    {
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    }

    private static void AddApiVersioning(IServiceCollection services)
    {
        services.AddApiVersioning(o =>
        {
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.DefaultApiVersion = new ApiVersion(1, 0);
        });

        services.AddVersionedApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });
    }
}
