namespace MovementPass.Public.Api.Stack
{
    using Amazon.CDK;

    using SysEnv = System.Environment;
    using CdkEnv = Amazon.CDK.Environment;

    public static class Program
    {
        public static void Main()
        {
            var app = new App();

            var env = new CdkEnv
            {
                Account = SysEnv.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = SysEnv.GetEnvironmentVariable("CDK_DEFAULT_REGION")
            };

            var prefix = (string)app.Node.TryGetContext("app");
            var version = (string)app.Node.TryGetContext("version");

            // ReSharper disable once ObjectCreationAsStatement
            new PublicApi(
                app,
                $"{prefix}-public-api-{version}",
                new StackProps { Env = env });

            app.Synth();
        }
    }
}
