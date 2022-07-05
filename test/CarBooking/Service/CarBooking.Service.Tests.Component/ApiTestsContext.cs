using CarBooking.Application.Repositories;
using CarBooking.Service.Consumers;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CarBooking.Service.Tests.Component;

public class ServiceTestsContext
{
    public Mock<ICarBookingRepository> MockCarBookingRepository { get; private set; }

    public ServiceTestsContext() => MockCarBookingRepository = new Mock<ICarBookingRepository>();

    public WebApplicationFactory<Startup> WebApplicationFactory
        => new WebApplicationFactory<Startup>()
            .WithWebHostBuilder(b => b.ConfigureServices(services
                => RegisterServices((ServiceCollection)services)));

    private void RegisterServices(ServiceCollection services)
        => services.AddMassTransitTestHarness(cfg => cfg.AddConsumer<BookingCreatedConsumer>())
            .AddSingleton(MockCarBookingRepository.Object);
}