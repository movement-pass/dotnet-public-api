namespace MovementPass.Public.Api;

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public static class Program
{
    public static async Task Main(string[] args) =>
        await CreateHostBuilder(args).Build().RunAsync()
            .ConfigureAwait(false);

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(builder =>
                builder.AddSystemsManager(
                    Environment.GetEnvironmentVariable("CONFIG_ROOT_KEY")))
            .ConfigureWebHostDefaults(builder =>
                builder.UseStartup<Startup>());
}