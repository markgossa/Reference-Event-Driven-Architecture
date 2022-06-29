using AspNet.CorrelationIdGenerator;
using Common.Messaging.Folder.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http;
using WebBff.Api;
using WebBff.Application.Infrastructure;
using WebBff.Domain.Models;

namespace WebBff.Tests.Component;

public class ApiTestsContext : IDisposable
{
    public HttpClient HttpClient { get; }
    public Mock<IMessageBusOutbox> MockMessageBusOutbox { get; } = new();
    public readonly string CorrelationId = Guid.NewGuid().ToString();
    public const string ErrorFirstName = "Unlucky";
    public const string DuplicateFirstName = "Duplicate"; 
    private readonly Mock<ICorrelationIdGenerator> _mockCorrelationIdGenerator = new();

    public ApiTestsContext()
    {
        HttpClient = BuildWebApplicationFactory().CreateClient();
        _mockCorrelationIdGenerator.Setup(m => m.Get()).Returns(CorrelationId);
        SetUpMockMessageBus();
    }

    private void SetUpMockMessageBus()
    {
        MockMessageBusOutbox.Setup(m => m.PublishBookingCreatedAsync(It.Is<Booking>(
                b => b.BookingSummary.FirstName == ErrorFirstName))).Throws<Exception>();
        
        MockMessageBusOutbox.Setup(m => m.PublishBookingCreatedAsync(It.Is<Booking>(
                b => b.BookingSummary.FirstName == DuplicateFirstName))).Throws<DuplicateMessageException>();
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
        services.AddSingleton(_mockCorrelationIdGenerator.Object);
        services.AddSingleton(MockMessageBusOutbox.Object);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}