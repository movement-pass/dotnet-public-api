namespace MovementPass.Public.Api.BackgroundJob.Infrastructure
{
    using System;
    using System.Text.Json;

    using Amazon.Lambda.KinesisEvents;

    public interface IRecordDeserializer
    {
        T Deserialize<T>(KinesisEvent.Record record);
    }

    public class RecordDeserializer : IRecordDeserializer
    {
        public T Deserialize<T>(KinesisEvent.Record record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return JsonSerializer.Deserialize<T>(record.Data.ToArray());
        }
    }
}
