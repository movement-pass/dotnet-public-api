namespace MovementPass.Public.Api.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

using NSubstitute;
using Xunit;

using Features.Login;
using Infrastructure;

public class LoginHandlerTests
{
    private const string MobilePhone = "01512345678";
    private static readonly DateTime DateOfBirth = new DateTime(1971, 12, 16);

    private readonly IAmazonDynamoDB _mockedDynamoDB;
    private readonly DynamoDBTablesOptions _tablesOptions;
    private readonly JwtOptions _jwtOptions;

    private readonly LoginHandler _handler;

    public  LoginHandlerTests()
    {
        this._tablesOptions = new DynamoDBTablesOptions
        {
            Applicants = "applicants",
            Passes = "passes"
        };

        this._jwtOptions = new JwtOptions
        {
            Audience = "movement-pass.com",
            Issuer = "movement-pass.com",
            Secret = string.Join(string.Empty, Enumerable.Repeat("$ecre8", 8)),
            Expiration = TimeSpan.Parse("00:01:00:00")
        };

        this._mockedDynamoDB = Substitute.For<IAmazonDynamoDB>();

        this._handler = new LoginHandler(
            this._mockedDynamoDB,
            new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions),
            new OptionsWrapper<JwtOptions>(this._jwtOptions));
    }

    [Fact]
    public void Constructor_throws_on_null_DynamoDB() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LoginHandler(
                null,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions),
                new OptionsWrapper<JwtOptions>(this._jwtOptions)));

    [Fact]
    public void Constructor_throws_on_null_DynamoDBTablesOptions() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LoginHandler(
                this._mockedDynamoDB,
                null,
                new OptionsWrapper<JwtOptions>(this._jwtOptions)));

    [Fact]
    public void Constructor_throws_on_null_JwtOptions() =>
        Assert.Throws<ArgumentNullException>(() =>
            new LoginHandler(
                this._mockedDynamoDB,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions),
                null));

    [Fact]
    public async Task Handle_returns_jwt_result()
    {
        this._mockedDynamoDB
            .GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = MobilePhone } },
                    { "name", new AttributeValue { S = "An applicant" } },
                    {
                        "dateOfBirth",
                        new AttributeValue
                        {
                            S = DateOfBirth.ToString(AWSSDKUtils.ISO8601DateFormat, CultureInfo.InvariantCulture)
                        }
                    },
                    {
                        "photo",
                        new AttributeValue
                        {
                            S = "https://photos.movement-pass.com/cab1e3c875bb4cc39bb0250598ca986a.png"
                        }
                    }
                }
            }));
        
        var result = await this._handler.Handle(new LoginRequest
            {
                MobilePhone = MobilePhone,
                DateOfBirth = DateOfBirth.ToString("ddMMyyyy")
            }, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_returns_null_for_nonexistent_applicant()
    {
        this._mockedDynamoDB
            .GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>()
            }));
        
        var result= await this._handler.Handle(new LoginRequest
            {
                MobilePhone = MobilePhone
            }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_returns_null_for_invalid_credentials()
    {
        this._mockedDynamoDB
            .GetItemAsync(Arg.Any<GetItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    { "id", new AttributeValue { S = MobilePhone } },
                    { "dateOfBirth", new AttributeValue
                    {
                        S = DateOfBirth.ToString(AWSSDKUtils.ISO8601DateFormat, CultureInfo.InvariantCulture)
                    } }
                }
            }));
        
        var result = await this._handler.Handle(new LoginRequest
            {
                MobilePhone = MobilePhone,
                DateOfBirth = DateOfBirth.AddDays(1).ToString("ddMMyyyy")
            }, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_throws_on_null_request() =>
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await this._handler.Handle(null, CancellationToken.None));
}
