namespace MovementPass.Public.Api.Stack
{
    using Amazon.CDK;
    using Amazon.CDK.AWS.Kinesis;

    public sealed class PassesStream : BaseStack
    {
        public PassesStream(
            Construct scope,
            string id,
            IStackProps props = null) : base(scope, id, props)
        {
            var stream = new Stream(this, "Stream",
                new StreamProps {
                    StreamName = $"{this.App}_passes_{this.Version}",
                    RetentionPeriod = Duration.Hours(24)
                });

            this.PutParameterStoreValue("kinesis/passes", stream.StreamArn);
        }
    }
}