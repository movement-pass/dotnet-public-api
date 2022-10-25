namespace MovementPass.Public.Api.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

using Moq;
using Xunit;

using Features.ViewPasses;
using Infrastructure;

public class ViewPassesHandlerTests
{
    private readonly Mock<IAmazonDynamoDB> _mockedDynamoDB;
    private readonly Mock<ICurrentUserProvider> _mockedCurrentUserProvider;
    private readonly DynamoDBTablesOptions _tablesOptions;

    private readonly ViewPassesHandler _handler;

    public ViewPassesHandlerTests()
    {
        this._tablesOptions = new DynamoDBTablesOptions
        {
            Applicants = "applicants",
            Passes = "passes"
        };

        this._mockedDynamoDB = new Mock<IAmazonDynamoDB>();
        this._mockedCurrentUserProvider = new Mock<ICurrentUserProvider>();

        this._handler = new ViewPassesHandler(
            this._mockedDynamoDB.Object,
            this._mockedCurrentUserProvider.Object,
            new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions));
    }

    [Fact]
    public void Constructor_throws_on_null_DynamoDB() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ViewPassesHandler(
                null,
                this._mockedCurrentUserProvider.Object,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions)));

    [Fact]
    public void Constructor_throws_on_null_CurrentUserProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ViewPassesHandler(
                this._mockedDynamoDB.Object, 
                null,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions)));

    [Fact]
    public void Constructor_throws_on_null_DynamoDBTablesOptions() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ViewPassesHandler(
                this._mockedDynamoDB.Object, 
                this._mockedCurrentUserProvider.Object,
                null));

    [Fact]
    public async Task Handle_returns_passes_that_belongs_to_calling_applicant()
    {
        var userId = IdGenerator.Generate();

        this._mockedCurrentUserProvider.Setup(cup => cup.UserId)
            .Returns(userId);

        QueryRequest req = null;

        this._mockedDynamoDB.Setup(ddb =>
                ddb.QueryAsync(
                    It.IsAny<QueryRequest>(),
                    It.IsAny<CancellationToken>()))
            .Callback<QueryRequest, CancellationToken>((r, _) => req = r)
            .Returns(Task.FromResult(new QueryResponse
            {
                Items = new List<Dictionary<string, AttributeValue>>
                {
                    new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { S = IdGenerator.Generate() } }
                    }
                },
                LastEvaluatedKey = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = IdGenerator.Generate() } },
                    { "userId", new AttributeValue { S = userId } },
                    {
                        "endAt",
                        new AttributeValue
                        {
                            S = Clock.Now().AddDays(1)
                                .ToString(AWSSDKUtils.ISO8601DateFormat, CultureInfo.InvariantCulture)
                        }
                    }
                }
            }));

        var result = await this._handler.Handle(new ViewPassesRequest
            {
                StartKey = new PassListKey
                {
                    Id = IdGenerator.Generate(),
                    EndAt = Clock.Now().ToString(AWSSDKUtils.ISO8601DateFormat, CultureInfo.InvariantCulture)
                }
            }, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Equal("applicantId", req.ExpressionAttributeNames["#aid"]);
        Assert.Equal(userId, req.ExpressionAttributeValues[":aid"].S);
        Assert.Equal(this._tablesOptions.Passes, req.TableName);
        Assert.Equal(25, req.Limit);

        Assert.NotEmpty(result.Passes);
        Assert.NotNull(result.NextKey);
    }

    [Fact]
    public async Task Handle_throws_on_null_request() =>
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await this._handler.Handle(null, CancellationToken.None));
}
