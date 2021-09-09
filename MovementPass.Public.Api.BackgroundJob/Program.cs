[assembly:
    Amazon.Lambda.Core.LambdaSerializer(
        typeof(Amazon.Lambda.Serialization.SystemTextJson.
            DefaultLambdaJsonSerializer))]

namespace MovementPass.Public.Api.BackgroundJob
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using Amazon.DynamoDBv2;
    using Amazon.Lambda.KinesisEvents;
    using Amazon.XRay.Recorder.Core;
    using Amazon.XRay.Recorder.Handlers.AwsSdk;

    using ExtensionMethods;
    using Infrastructure;
    using Services;

    public class Program
    {
        private static readonly ServiceProvider Container = CreateContainer();

        public async Task Main(KinesisEvent kinesisEvent)
        {
            if (kinesisEvent == null)
            {
                throw new ArgumentNullException(nameof(kinesisEvent));
            }

            await Container.GetRequiredService<IProcessor>()
                .Process(kinesisEvent.Records, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private static ServiceProvider CreateContainer()
        {
            var config = new ConfigurationBuilder()
                .AddSystemsManager(
                    Environment.GetEnvironmentVariable("CONFIG_ROOT_KEY"))
                .Build();

            var services = new ServiceCollection();

            services.AddOptions();
            services.AddSingleton<IConfiguration>(_ => config);

            services.AddDefaultAWSOptions(config.GetAWSOptions());
            services.AddAWSService<IAmazonDynamoDB>();

            var production = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

            if (production)
            {
                AWSXRayRecorder.InitializeInstance(config);
                AWSSDKHandler.RegisterXRayForAllServices();
            }

            config.Apply<DynamoDBTablesOptions>(services);
            config.Apply<JwtOptions>(services);

            services.AddSingleton<IRecordDeserializer, RecordDeserializer>();
            services.AddSingleton<ITokenValidator, TokenValidator>();
            services.AddSingleton<IDataLoader, DataLoader>();
            services.AddSingleton<IProcessor, Processor>();

            return services.BuildServiceProvider();
        }
    }
}