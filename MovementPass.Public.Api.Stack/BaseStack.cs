namespace MovementPass.Public.Api.Stack;

using Amazon.CDK;
using Amazon.CDK.AWS.SSM;

public abstract class BaseStack : Stack
{
    protected BaseStack(
        Construct scope,
        string id,
        IStackProps props = null) : base(scope, id, props)
    {
    }

    protected string App => this.GetContextValue<string>("app");

    protected string Version => this.GetContextValue<string>("version");

    protected string Domain => this.GetContextValue<string>("domain");

    protected string ConfigRootKey => $"/{this.App}/{this.Version}";

    protected T GetContextValue<T>(string key) =>
        (T)this.Node.TryGetContext(key);

    protected void PutParameterStoreValue(string name, string value) =>
        // ReSharper disable once ObjectCreationAsStatement
        new StringParameter(this, $"{name}Parameter",
            new StringParameterProps {
                ParameterName = $"{this.ConfigRootKey}/{name}",
                StringValue = value
            });

    protected string GetParameterStoreValue(string name) =>
        StringParameter.ValueForStringParameter(
            this,
            $"{this.ConfigRootKey}/{name}");
}