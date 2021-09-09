namespace MovementPass.Public.Api.BackgroundJob.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    using Amazon.Lambda.KinesisEvents;

    using ExtensionMethods;
    using Infrastructure;

    public interface IDataReducer
    {
        IEnumerable<Pass> Reduce(
            IEnumerable<KinesisEvent.KinesisEventRecord> records);
    }

    public class DataReducer : IDataReducer
    {
        private readonly IRecordDeserializer _deserializer;
        private readonly ITokenValidator _tokenValidator;

        public DataReducer(IRecordDeserializer deserializer,
            ITokenValidator tokenValidator)
        {
            this._deserializer = deserializer ??
                                 throw new ArgumentNullException(
                                     nameof(deserializer));

            this._tokenValidator = tokenValidator ??
                                   throw new ArgumentNullException(
                                       nameof(tokenValidator));
        }

        public IEnumerable<Pass> Reduce(
            IEnumerable<KinesisEvent.KinesisEventRecord> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            return records
                .Select(record =>
                    this._deserializer
                        .Deserialize<ApplyRequest>(record.Kinesis))
                .Where(request =>
                    Validator.TryValidateObject(
                        request,
                        new ValidationContext(request),
                        new List<ValidationResult>()))
                .Where(request =>
                {
                    var userId = this._tokenValidator.Validate(request.Token);

                    if (string.IsNullOrEmpty(userId))
                    {
                        return false;
                    }

                    request.ApplicantId = userId;

                    return true;
                })
                .Select(request =>
                {
                    var pass = new Pass
                    {
                        Id = IdGenerator.Generate(),
                        StartAt = request.DateTime,
                        EndAt =
                            request.DateTime.AddHours(request.DurationInHour),
                        CreatedAt = Clock.Now(),
                        Status = "APPLIED"
                    }.Merge(request);

                    if (!pass.IncludeVehicle)
                    {
                        pass.VehicleNo = null;
                        pass.SelfDriven = false;
                        pass.DriverName = null;
                        pass.DriverLicenseNo = null;
                    }

                    if (pass.SelfDriven)
                    {
                        pass.DriverName = null;
                        pass.DriverLicenseNo = null;
                    }

                    return pass;
                })
                .ToList();
        }
    }
}