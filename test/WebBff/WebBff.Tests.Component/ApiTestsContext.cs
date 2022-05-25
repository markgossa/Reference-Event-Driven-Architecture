using Common.CorrelationIdGenerator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http;
using WebBff.Api;
using WebBff.Application.Repositories;
using WebBff.Domain.Models;

namespace WebBff.Tests.Component;

public class ApiTestsContext : IDisposable
{
    public HttpClient HttpClient { get; }
    public Mock<IBookingRepository> MockBookingRepository { get; } = new();
    public readonly string CorrelationId = Guid.NewGuid().ToString();

    protected const string _errorFirstName = "Unlucky";
    private readonly Mock<ICorrelationIdGenerator> _mockCorrelationIdGenerator = new();

    public ApiTestsContext()
    {
        HttpClient = BuildWebApplicationFactory().CreateClient();
        _mockCorrelationIdGenerator.Setup(m => m.CorrelationId).Returns(CorrelationId);
        SetUpMockBookingRepository();
    }

    private void SetUpMockBookingRepository()
        => MockBookingRepository.Setup(m => m.SendBookingAsync(It.Is<Booking>(
            b => b.FirstName == _errorFirstName))).Throws<Exception>();

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
        services.AddSingleton(_mockCorrelationIdGenerator.Object);
        services.AddSingleton(MockBookingRepository.Object);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}