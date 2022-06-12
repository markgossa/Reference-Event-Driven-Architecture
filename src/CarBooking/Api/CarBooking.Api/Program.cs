using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace LSE.Stocks.Api;

public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();
    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}