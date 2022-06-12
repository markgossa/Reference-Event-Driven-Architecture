using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http;
using AspNet.CorrelationIdGenerator;
using Microsoft.Extensions.Hosting;

namespace CarBooking.Api.Tests.Component;

public class ApiTestsContext : IDisposable
{
    public Mock<ICarBookingService> MockCarBookingRequest { get; } = new();
    public HttpClient HttpClient { get; }
    private readonly Mock<ICorrelationIdGenerator> _mockCorrelationIdGenerator = new();
    public readonly string CorrelationId = Guid.NewGuid().ToString();

    public ApiTestsContext()
    {
        HttpClient = BuildWebApplicationFactory().CreateClient();
        SetUpMockCarBookingRequest();
        _mockCorrelationIdGenerator.Setup(m => m.Get()).Returns(CorrelationId);
    }

    private void SetUpMockCarBookingRequest()
    {
        MockCarBookingRequest.Setup(m => m.BookAsync(It.Is<CarBooking>(t => t.FirstName == "Unlucky"), It.IsAny<string>()))
            .ThrowsAsync(new Exception());

        MockCarBookingRequest.Setup(m => m.BookAsync(It.Is<CarBooking>(t => t.FirstName == "Duplicate"), It.IsAny<string>()))
            .ThrowsAsync(new DuplicateMessageException());
    }

    protected WebApplicationFactory<Startup> BuildWebApplicationFactory()
        => new WebApplicationFactory<Startup>()
            .WithWebHostBuilder(b =>
            {
                b.ConfigureAppConfiguration((context, configBuilder) => DisableSwaggerWhenTesting(configBuilder));

                b.ConfigureServices(services => RegisterServices((ServiceCollection)services));
            });

    private static void DisableSwaggerWhenTesting(IConfigurationBuilder configBuilder)
        => configBuilder.AddInMemoryCollection(new Dictionary<string, string> { ["EnableSwagger"] = "false" });

    private void RegisterServices(ServiceCollection services)
    {
        services.AddSingleton(MockCarBookingRequest.Object);
        services.AddSingleton(_mockCorrelationIdGenerator.Object);
        RemoveHostedServices(services);
    }

    private static void RemoveHostedServices(ServiceCollection services)
    {
        foreach (var hostedService in GetHostedServices(services))
        {
            services.Remove(hostedService);
        }
    }

    private static List<ServiceDescriptor> GetHostedServices(ServiceCollection services)
        => services.Where(s => s.ServiceType == typeof(IHostedService)
                && s.ImplementationType?.Name != "Microsoft.AspNetCore.Hosting.GenericWebHostService")
            .ToList();

    public void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}