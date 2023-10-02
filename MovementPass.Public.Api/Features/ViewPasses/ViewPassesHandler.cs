namespace MovementPass.Public.Api.Features.ViewPasses;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using MediatR;

using Entities;
using ExtensionMethods;
using Infrastructure;

public class ViewPassesHandler :
    IRequestHandler<ViewPassesRequest, PassListResult>
{
    private readonly IAmazonDynamoDB _dynamodb;
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly DynamoDBTablesOptions _tableOptions;

    public ViewPassesHandler(
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

    public async Task<PassListResult> Handle(ViewPassesRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var userId = this._currentUserProvider.UserId;

        var req = new QueryRequest
        {
            TableName = this._tableOptions.Passes,
            IndexName = "ix_applicantId-endAt",
            KeyConditionExpression = "#aid = :aid",
            ExpressionAttributeNames =
                new Dictionary<string, string> { { "#aid", "applicantId" } },
            ExpressionAttributeValues =
                new Dictionary<string, AttributeValue>
                {
                    { ":aid", new AttributeValue { S = userId } }
                },
            Limit = 25,
            ScanIndexForward = false,
            ReturnConsumedCapacity = ReturnConsumedCapacity.NONE
        };

        if (request.StartKey != null)
        {
            if (!string.IsNullOrEmpty(request.StartKey.Id) &&
                !string.IsNullOrEmpty(request.StartKey.EndAt))
            {
                req.ExclusiveStartKey =
                    request.StartKey.ToDynamoDBAttributes();
                req.ExclusiveStartKey.Add("applicantId",
                    new AttributeValue { S = userId });
            }
        }

        var res = await this._dynamodb.QueryAsync(req, cancellationToken)
            .ConfigureAwait(false);

        PassListKey nextKey = null;

        if (res.LastEvaluatedKey.Any())
        {
            nextKey = res.LastEvaluatedKey
                .FromDynamoDBAttributes<PassListKey>();
        }

        var passes = res.Items
            .Select(pass => pass.FromDynamoDBAttributes<Pass>())
            .Select(pass => new PassItem().Merge(pass))
            .ToList();

        return new PassListResult { Passes = passes, NextKey = nextKey };
    }
}