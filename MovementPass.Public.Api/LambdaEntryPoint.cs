namespace MovementPass.Public.Api
{
    using System;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    using Amazon.Lambda.AspNetCoreServer;

    public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
    {
        protected override void Init(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration(
                    options => options.AddSystemsManager(
                        Environment.GetEnvironmentVariable("CONFIG_ROOT_KEY")))
                .UseStartup<Startup>();
    }
}