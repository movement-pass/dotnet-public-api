namespace MovementPass.Public.Api.BackgroundJob.Infrastructure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.Json;

    using Amazon.Lambda.KinesisEvents;
    using Microsoft.Extensions.Logging;

    public interface IRecordDeserializer
    {
        T Deserialize<T>(KinesisEvent.Record record);
    }

    public class RecordDeserializer : IRecordDeserializer
    {
        private readonly ILogger<RecordDeserializer> _logger;

        public RecordDeserializer(ILogger<RecordDeserializer> logger) =>
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public T Deserialize<T>(KinesisEvent.Record record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            using var reader = new StreamReader(record.Data);
            var payload = reader.ReadToEnd();

            this._logger.LogInformation("Payload: {@payload}", payload);

            return JsonSerializer.Deserialize<T>(payload);
        }
    }
}
