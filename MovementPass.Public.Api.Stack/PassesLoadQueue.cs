namespace MovementPass.Public.Api.Stack
{
    using Amazon.CDK;
    using Amazon.CDK.AWS.SQS;

    public sealed class PassesLoadQueue : BaseStack
    {
        public PassesLoadQueue(
            Construct scope,
            string id,
            IStackProps props = null) : base(scope, id, props) =>

            // ReSharper disable once ObjectCreationAsStatement
            new Queue(this, "LoadQueue",
                new QueueProps
                {
                    QueueName = $"{this.App}_passes_load_{this.Version}.fifo",
                    ReceiveMessageWaitTime = Duration.Seconds(20),
                    VisibilityTimeout = Duration.Minutes(5),
                    Fifo = true,
                    ContentBasedDeduplication = true,
                    FifoThroughputLimit = FifoThroughputLimit.PER_MESSAGE_GROUP_ID,
                    DeduplicationScope = DeduplicationScope.MESSAGE_GROUP,
                });
    }
}