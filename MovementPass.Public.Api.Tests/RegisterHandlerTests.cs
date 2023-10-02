namespace MovementPass.Public.Api.Tests;

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

using Features.Register;
using Infrastructure;

public class RegisterHandlerTests
{
    private const string MobilePhone = "01512345678";

    private readonly IAmazonDynamoDB _mockedDynamoDB;
    private readonly DynamoDBTablesOptions _tablesOptions;
    private readonly JwtOptions _jwtOptions;

    private readonly RegisterHandler _handler;

    public RegisterHandlerTests()
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

        this._handler = new RegisterHandler(
            this._mockedDynamoDB,
            new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions),
            new OptionsWrapper<JwtOptions>(this._jwtOptions));
    }

    [Fact]
    public void Constructor_throws_on_null_DynamoDB() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RegisterHandler(
                null,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions),
                new OptionsWrapper<JwtOptions>(this._jwtOptions)));
        
    [Fact]
    public void Constructor_throws_on_null_DynamoDBTablesOptions() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RegisterHandler(
                this._mockedDynamoDB,
                null,
                new OptionsWrapper<JwtOptions>(this._jwtOptions)));

    [Fact]
    public void Constructor_throws_on_null_JwtOptions() =>
        Assert.Throws<ArgumentNullException>(() =>
            new RegisterHandler(
                this._mockedDynamoDB,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions),
                null));

    [Fact]
    public async Task Handle_returns_jwt_result()
    {
        PutItemRequest req = null;
        
        this._mockedDynamoDB.PutItemAsync(
                Arg.Any<PutItemRequest>(),
                Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(x =>
            {
                req = x.Arg<PutItemRequest>();
                
                return Task.FromResult(new PutItemResponse());
            });
        
        var input = new RegisterRequest
        {
            Name = "An Applicant",
            MobilePhone = MobilePhone,
            District = 9876,
            Thana = 12345,
            Gender = "O",
            DateOfBirth = Clock.Now().AddYears(-19),
            IdType = "PP",
            IdNumber = "1234567890",
            Photo = "https://photos.movement-pass.com/123456.jpg"
        };

        var result = await this._handler.Handle(input, CancellationToken.None);

        Assert.Equal(input.MobilePhone, req.Item["id"].S);
        Assert.Equal(input.Name, req.Item["name"].S);
        Assert.Equal(input.Name, req.Item["name"].S);
        Assert.Equal(input.District.ToString(CultureInfo.InvariantCulture), req.Item["district"].N);
        Assert.Equal(input.Thana.ToString(CultureInfo.InvariantCulture), req.Item["thana"].N);
        Assert.Equal(
            input.DateOfBirth.ToString(AWSSDKUtils.ISO8601DateFormat, CultureInfo.InvariantCulture),
            req.Item["dateOfBirth"].S);
        Assert.Equal(input.Gender, req.Item["gender"].S);
        Assert.Equal(input.IdType, req.Item["idType"].S);
        Assert.Equal(input.IdNumber, req.Item["idNumber"].S);
        Assert.Equal(input.Photo, req.Item["photo"].S);
        Assert.NotEmpty(req.Item["createdAt"].S);
        Assert.NotEmpty(req.ConditionExpression);
        Assert.Equal(this._tablesOptions.Applicants, req.TableName);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_returns_null_when_applicant_already_exists()
    {
        this._mockedDynamoDB.PutItemAsync(
                Arg.Any<PutItemRequest>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConditionalCheckFailedException("Failed"));
        
        var applicant = await this._handler.Handle(new RegisterRequest(), CancellationToken.None);

        Assert.Null(applicant);
    }

    [Fact]
    public async Task Handle_throws_on_null_request() =>
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await this._handler.Handle(null, CancellationToken.None));
}
