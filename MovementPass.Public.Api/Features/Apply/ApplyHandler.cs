namespace MovementPass.Public.Api.Features.Apply;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using MediatR;

using Entities;
using ExtensionMethods;
using Infrastructure;

public class ApplyHandler : IRequestHandler<ApplyRequest, IdResult>
{
    private readonly IAmazonDynamoDB _dynamodb;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly DynamoDBTablesOptions _tableOptions;

    public ApplyHandler(
        IAmazonDynamoDB dynamodb,
        ICurrentUserProvider currentUserProvider,
        IOptions<DynamoDBTablesOptions> tableOptions)
    {
        if (tableOptions == null)
        {
            throw new ArgumentNullException(nameof(tableOptions));
        }

        this._dynamodb = dynamodb ??
                         throw new ArgumentNullException(nameof(dynamodb));

        this._currentUserProvider = currentUserProvider ??
                                    throw new ArgumentNullException(
                                        nameof(currentUserProvider));

        this._tableOptions = tableOptions.Value;
    }

    public async Task<IdResult> Handle(
        ApplyRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userId = this._currentUserProvider.UserId;

        var pass = new Pass
        {
            Id = IdGenerator.Generate(),
            StartAt = request.DateTime,
            EndAt = request.DateTime.AddHours(request.DurationInHour),
            CreatedAt = Clock.Now(),
            Status = "APPLIED",
            ApplicantId = userId
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

        var req = new TransactWriteItemsRequest
        {
            TransactItems = new List<TransactWriteItem>
            {
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = this._tableOptions.Passes,
                        Item = pass.ToDynamoDBAttributes()
                    }
                },
                new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = this._tableOptions.Applicants,
                        Key =
                            new Dictionary<string, AttributeValue>
                            {
                                { "id", new AttributeValue { S = userId } }
                            },
                        UpdateExpression = "SET #ac = #ac + :inc",
                        ExpressionAttributeNames =
                            new Dictionary<string, string>
                            {
                                { "#ac", "appliedCount" }
                            },
                        ExpressionAttributeValues =
                            new Dictionary<string, AttributeValue>
                            {
                                { ":inc", new AttributeValue { N = "1" } }
                            }
                    }
                }
            }
        };

        await this._dynamodb.TransactWriteItemsAsync(req, cancellationToken)
            .ConfigureAwait(false);

        return new IdResult
        {
            Id = pass.Id
        };
    }
}