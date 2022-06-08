﻿using BookingGenerator.Api;
using BookingGenerator.Application.Repositories;
using BookingGenerator.Domain.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http;
using AspNet.CorrelationIdGenerator;
using Common.Messaging.Folder.Models;

namespace BookingGenerator.Tests.Component;

public class ApiTestsContext : IDisposable
{
    public Mock<IBookingService> MockBookingService { get; } = new();
    public HttpClient HttpClient { get; }
    private readonly Mock<ICorrelationIdGenerator> _mockCorrelationIdGenerator = new();
    public readonly string CorrelationId = Guid.NewGuid().ToString();

    public ApiTestsContext()
    {
        HttpClient = BuildWebApplicationFactory().CreateClient();
        SetUpMockBookingService();
        _mockCorrelationIdGenerator.Setup(m => m.Get()).Returns(CorrelationId);
    }

    private void SetUpMockBookingService()
    {
        MockBookingService.Setup(m => m.BookAsync(It.Is<Booking>(t => t.FirstName == "Unlucky"), It.IsAny<string>()))
            .ThrowsAsync(new Exception());
        
        MockBookingService.Setup(m => m.BookAsync(It.Is<Booking>(t => t.FirstName == "Duplicate"), It.IsAny<string>()))
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
        services.AddSingleton(MockBookingService.Object);
        services.AddSingleton(_mockCorrelationIdGenerator.Object);
    }

    public void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}