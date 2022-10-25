namespace MovementPass.Public.Api.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Moq;
using Xunit;

using Features.ViewPass;
using Infrastructure;

public class ViewPassHandlerTests
{
    private readonly Mock<IAmazonDynamoDB> _mockedDynamoDB;
    private readonly Mock<ICurrentUserProvider> _mockedCurrentUserProvider;
    private readonly DynamoDBTablesOptions _tablesOptions;

    private readonly ViewPassHandler _handler;

    public ViewPassHandlerTests()
    {
        this._tablesOptions = new DynamoDBTablesOptions
        {
            Applicants = "applicants",
            Passes = "passes"
        };

        this._mockedDynamoDB = new Mock<IAmazonDynamoDB>();
        this._mockedCurrentUserProvider = new Mock<ICurrentUserProvider>();

        this._handler = new ViewPassHandler(
            this._mockedDynamoDB.Object,
            this._mockedCurrentUserProvider.Object,
            new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions));
    }

    [Fact]
    public void Constructor_throws_on_null_DynamoDB() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ViewPassHandler(
                null,
                this._mockedCurrentUserProvider.Object,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions)));

    [Fact]
    public void Constructor_throws_on_null_CurrentUserProvider() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ViewPassHandler(
                this._mockedDynamoDB.Object, 
                null,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions)));
    [Fact]
    public void Constructor_throws_on_null_DynamoDBTablesOptions() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ViewPassHandler(
                this._mockedDynamoDB.Object, 
                this._mockedCurrentUserProvider.Object,
                null));
    [Fact]
    public async Task Handle_returns_matching_pass()
    {
        var id = IdGenerator.Generate();
        var userId = IdGenerator.Generate();

        this._mockedCurrentUserProvider.Setup(cup => cup.UserId).Returns(userId);

        this._mockedDynamoDB.SetupSequence(ddb =>
                ddb.GetItemAsync(
                    It.IsAny<GetItemRequest>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = id } },
                    { "applicantId", new AttributeValue { S = userId } }
                }
            }))
            .Returns(Task.FromResult(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = userId } }
                }
            }));

        var pass = await this._handler.Handle(new ViewPassRequest { Id = id }, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.NotNull(pass);
        Assert.Equal(id, pass.Id);
        Assert.NotNull(pass.Applicant);
        Assert.Equal(userId, pass.Applicant.Id);
    }

    [Fact]
    public async Task Handle_returns_null_on_nonexistent_pass()
    {
        this._mockedCurrentUserProvider.Setup(cup => cup.UserId)
            .Returns(IdGenerator.Generate);

        this._mockedDynamoDB.Setup(ddb =>
                ddb.GetItemAsync(
                    It.IsAny<GetItemRequest>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>()
            }));

        var res = await this._handler
            .Handle(new ViewPassRequest { Id = IdGenerator.Generate() }, CancellationToken.None)
            .ConfigureAwait(false);

        Assert.Null(res);
    }

    [Fact]
    public async Task Handle_throws_on_null_request() =>
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await this._handler.Handle(null, CancellationToken.None));
}
