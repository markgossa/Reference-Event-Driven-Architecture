using CarBooking.Application.Repositories;
using CarBooking.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Net.Http;

namespace CarBooking.Api.Tests.Component;

public class ApiTestsContext : IDisposable
{
    public Mock<ICarBookingRepository> MockCarBookingRepository { get; } = new();
    public HttpClient HttpClient { get; }
    public const string ErrorBooking = "Unlucky";
    public const string DuplicateBooking = "Duplicate";

    public ApiTestsContext()
    {
        HttpClient = BuildWebApplicationFactory().CreateClient();
        SetUpMockCarBookingRequest();
    }

    private void SetUpMockCarBookingRequest()
    {
        MockCarBookingRepository.Setup(m => m.SaveAsync(It.Is<Domain.Models.CarBooking>(t => t.FirstName == ErrorBooking)))
            .ThrowsAsync(new Exception());

        MockCarBookingRepository.Setup(m => m.SaveAsync(It.Is<Domain.Models.CarBooking>(t => t.FirstName == DuplicateBooking)))
            .ThrowsAsync(new DuplicateBookingException());
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
        services.AddSingleton(MockCarBookingRepository.Object);
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