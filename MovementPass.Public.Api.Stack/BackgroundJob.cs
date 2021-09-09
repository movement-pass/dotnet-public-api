namespace MovementPass.Public.Api.Stack
{
    using System.Collections.Generic;

    using Amazon.CDK;
    using Amazon.CDK.AWS.Kinesis;
    using Amazon.CDK.AWS.Lambda;
    using Amazon.CDK.AWS.Lambda.EventSources;

    public class BackgroundJob : BaseStack
    {
        public BackgroundJob(
            Construct scope,
            string id,
            IStackProps props = null) : base(scope, id, props)
        {
            var name = $"{this.App}_public-api-background-job_{this.Version}";

            var passesStreamArn = this.GetParameterStoreValue("kinesis/passes");
            var stream = Stream.FromStreamArn(this, "Stream", passesStreamArn);

            var lambda = new Function(this, "Lambda",
                new FunctionProps {
                    FunctionName = name,
                    Handler =
                        "MovementPass.Public.Api.BackgroundJob::MovementPass.Public.Api.BackgroundJob.Program::Main",
                    Runtime = Runtime.DOTNET_CORE_3_1,
                    Timeout = Duration.Minutes(15),
                    MemorySize = 3008,
                    Code = Code.FromAsset($"dist/{name}.zip"),
                    Tracing = Tracing.ACTIVE,
                    Environment = new Dictionary<string, string> {
                        { "ASPNETCORE_ENVIRONMENT", "Production" },
                        { "CONFIG_ROOT_KEY", this.ConfigRootKey }
                    }
                });

            lambda.AddEventSource(new KinesisEventSource(stream,
                new KinesisEventSourceProps {
                    BatchSize = 1000,
                    MaxBatchingWindow = Duration.Minutes(1),
                    StartingPosition = StartingPosition.LATEST
                }));

            stream.GrantRead(lambda);
        }
    }
}