using CarBooking.Api.ApplicationBuilderExtensions;
using CarBooking.Api.ServiceCollectionExtensions;
using CarBooking.Api.Swagger;
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

namespace CarBooking.Api;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration) => Configuration = configuration;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer()
            .AddMediatorServices()
            .AddApplicationInsightsTelemetry()
            .AddServices(Configuration)
            .AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerGenOptions>()
            .AddControllers();

        AddSwagger(services);
        AddApiVersioning(services);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env,
        IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    {
        if (env.IsDevelopment() && IsSwaggerEnabled())
        {
            app.UseSwagger();
            app.UseSwaggerUI(o =>
            {
                foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
                {
                    o.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                        $"GreatEscapes.CarBookings.Api - {description.GroupName.ToUpper()}");
                }
            });
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();
        app.UseCustomExceptionHandlerMiddleware();

        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }

    private bool IsSwaggerEnabled()
        => bool.TryParse(Configuration["EnableSwagger"], out var enableSwagger) && enableSwagger;

    private IServiceCollection AddSwagger(IServiceCollection services)
    {
        if (IsSwaggerEnabled())
        {
            services.AddSwaggerGen(o => ConfigureSwaggerGenOptions(o));
        }

        return services;
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
