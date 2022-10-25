namespace MovementPass.Public.Api.Features.Login;

using System;
using System.Collections.Generic;
using System.Globalization;
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

public class LoginHandler : IRequestHandler<LoginRequest, JwtResult>
{
    private readonly IAmazonDynamoDB _dynamodb;
    private readonly DynamoDBTablesOptions _tableOptions;
    private readonly JwtOptions _jwtOptions;

    public LoginHandler(
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
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var req = new GetItemRequest {
            TableName = this._tableOptions.Applicants,
            Key = new Dictionary<string, AttributeValue> {
                { "id", new AttributeValue { S = request.MobilePhone } }
            }
        };

        var res = await this._dynamodb.GetItemAsync(
                req,
                cancellationToken)
            .ConfigureAwait(false);

        var applicant = res.Item.Any()
            ? res.Item.FromDynamoDBAttributes<Applicant>()
            : null;

        if (applicant == null || !string.Equals(
                applicant.DateOfBirth.ToString(@"ddMMyyyy",
                    CultureInfo.InvariantCulture), request.DateOfBirth.Trim(),
                StringComparison.Ordinal))
        {
            return null;
        }

        return applicant.GenerateJwt(this._jwtOptions);
    }
}