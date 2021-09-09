namespace MovementPass.Public.Api.Stack
{
    using System;
    using System.Linq;
    using System.Reflection;

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

            var stackTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => typeof(Stack).IsAssignableFrom(type))
                .ToList();

            var prefix = (string)app.Node.TryGetContext("app");
            var version = (string)app.Node.TryGetContext("version");

            foreach (var type in stackTypes)
            {
                Activator.CreateInstance(
                    type,
                    app,
                    $"{prefix}-{type.Name}-{version}".ToLowerInvariant(),
                    new StackProps { Env = env });
            }

            app.Synth();
        }
    }
}
