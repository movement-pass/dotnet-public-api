namespace MovementPass.Public.Api.Features.Register;

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

public class RegisterHandler : IRequestHandler<RegisterRequest, JwtResult>
{
    private readonly IAmazonDynamoDB _dynamodb;
    private readonly DynamoDBTablesOptions _tableOptions;
    private readonly JwtOptions _jwtOptions;

    public RegisterHandler (
        IAmazonDynamoDB dynamodb,
        IOptions<DynamoDBTablesOptions> tableOptions,
        IOptions<JwtOptions> jwtOptions)
    {
        if (tableOptions == null)
        {
            throw new ArgumentNullException(nameof(tableOptions));
        }

        if (jwtOptions == null)
        {
            throw new ArgumentNullException(nameof(jwtOptions));
        }

        this._dynamodb = dynamodb ??
                         throw new ArgumentNullException(nameof(dynamodb));
        this._tableOptions = tableOptions.Value;
        this._jwtOptions = jwtOptions.Value;
    }

    public async Task<JwtResult> Handle(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var applicant = new Applicant
        {
            Id = request.MobilePhone,
            CreatedAt = Clock.Now()
        }.Merge(request);

        var req = new PutItemRequest
        {
            TableName = this._tableOptions.Applicants,
            Item = applicant.ToDynamoDBAttributes(),
            ConditionExpression = "attribute_not_exists(#i)",
            ExpressionAttributeNames =
                new Dictionary<string, string> { { "#i", "id" } }
        };

        try
        {
            await this._dynamodb.PutItemAsync(req, cancellationToken)
                .ConfigureAwait(false);

            return applicant.GenerateJwt(this._jwtOptions);
        }
        catch (ConditionalCheckFailedException)
        {
            return null;
        }
    }
}