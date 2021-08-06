namespace MovementPass.Public.Api.Tests
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Options;

    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.Model;
    using Amazon.Util;

    using Moq;
    using Xunit;

    using Features.Apply;
    using Infrastructure;

    public class ApplyHandlerTests
    {
        private readonly Mock<IAmazonDynamoDB> _mockedDynamoDB;
        private readonly Mock<ICurrentUserProvider> _mockedCurrentUserProvider;
        private readonly DynamoDBTablesOptions _tablesOptions;

        private readonly ApplyHandler _handler;

        public ApplyHandlerTests()
        {
            this._tablesOptions = new DynamoDBTablesOptions
            {
                Applicants = "applicants",
                Passes = "passes"
            };

            this._mockedDynamoDB = new Mock<IAmazonDynamoDB>();
            this._mockedCurrentUserProvider = new Mock<ICurrentUserProvider>();

            this._handler = new ApplyHandler(
                this._mockedDynamoDB.Object,
                this._mockedCurrentUserProvider.Object,
                new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions));
        }

        [Fact]
        public void Constructor_throws_on_null_DynamoDB() =>
            Assert.Throws<ArgumentNullException>(() =>
                new ApplyHandler(
                    null,
                    this._mockedCurrentUserProvider.Object,
                    new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions)));

        [Fact]
        public void Constructor_throws_on_null_CurrentUserProvider() =>
            Assert.Throws<ArgumentNullException>(() =>
                new ApplyHandler(
                    this._mockedDynamoDB.Object,
                    null,
                    new OptionsWrapper<DynamoDBTablesOptions>(this._tablesOptions)));

        [Fact]
        public void Constructor_throws_on_null_DynamoDBTablesOptions() =>
            Assert.Throws<ArgumentNullException>(() =>
                new ApplyHandler(
                    this._mockedDynamoDB.Object,
                    this._mockedCurrentUserProvider.Object,
                    null));

        [Theory]
        [InlineData(true, true, 1)]
        [InlineData(true, false, 5)]
        [InlineData(false, true, 8)]
        [InlineData(false, false, 12)]
        public async Task Handle_returns_new_pass_id(bool includeVehicle, bool selfDriven, int durationInHour)
        {
            var userId = IdGenerator.Generate();

            this._mockedCurrentUserProvider.Setup(cup => cup.UserId).Returns(userId);

            TransactWriteItemsRequest req = null;

            this._mockedDynamoDB.Setup(ddb =>
                    ddb.TransactWriteItemsAsync(
                        It.IsAny<TransactWriteItemsRequest>(),
                        It.IsAny<CancellationToken>()))
                .Callback<TransactWriteItemsRequest, CancellationToken>((r, _) => req = r)
                .ReturnsAsync(new TransactWriteItemsResponse());

            var input = new ApplyRequest
            {
                FromLocation = "Location A",
                ToLocation = "Location B",
                District = 123,
                Thana = 456,
                DateTime = Clock.Now().AddDays(1),
                DurationInHour = durationInHour,
                Type = "R",
                Reason = "A reason",
                IncludeVehicle = includeVehicle,
                VehicleNo = "Dhaka metro x-xx-xxxx",
                SelfDriven = selfDriven,
                DriverName = "My Driver",
                DriverLicenseNo = new string('x', 32)
            };

            var res = await this._handler.Handle(input, CancellationToken.None)
                .ConfigureAwait(false);

            var put = req.TransactItems[0].Put;

            Assert.Equal(this._tablesOptions.Passes, put.TableName);
            Assert.Equal(input.FromLocation, put.Item["fromLocation"].S);
            Assert.Equal(input.ToLocation, put.Item["toLocation"].S);
            Assert.Equal(input.District.ToString(CultureInfo.InvariantCulture), put.Item["district"].N);
            Assert.Equal(input.Thana.ToString(CultureInfo.InvariantCulture), put.Item["thana"].N);
            Assert.Equal(input.Type, put.Item["type"].S);
            Assert.Equal(input.Reason, put.Item["reason"].S);
            Assert.Equal(input.IncludeVehicle, put.Item["includeVehicle"].BOOL);
            Assert.Equal(
                input.DateTime.ToString(AWSSDKUtils.ISO8601DateFormat, CultureInfo.InvariantCulture),
                put.Item["startAt"].S);
            Assert.Equal(
                input.DateTime.AddHours(durationInHour)
                    .ToString(AWSSDKUtils.ISO8601DateFormat, CultureInfo.InvariantCulture),
                put.Item["endAt"].S);
            Assert.Equal("APPLIED", put.Item["status"].S);
            Assert.Equal(userId, put.Item["applicantId"].S);
            Assert.NotEmpty(put.Item["id"].S);
            Assert.NotEmpty(put.Item["createdAt"].S);

            if (includeVehicle)
            {
                Assert.Equal(input.VehicleNo, put.Item["vehicleNo"].S);
                Assert.Equal(input.SelfDriven, put.Item["selfDriven"].BOOL);

                if (selfDriven)
                {
                    Assert.False(put.Item.ContainsKey("driverName"));
                    Assert.False(put.Item.ContainsKey("driverLicenseNo"));
                }
                else
                {
                    Assert.Equal(input.DriverName, put.Item["driverName"].S);
                    Assert.Equal(input.DriverLicenseNo, put.Item["driverLicenseNo"].S);
                }
            }
            else
            {
                Assert.False(put.Item["selfDriven"].BOOL);
                Assert.False(put.Item.ContainsKey("driverName"));
                Assert.False(put.Item.ContainsKey("driverLicenseNo"));
            }

            var update = req.TransactItems[1].Update;

            Assert.Equal(this._tablesOptions.Applicants, update.TableName);
            Assert.NotEmpty(update.UpdateExpression);

            Assert.NotEmpty(res.Id);
        }

        [Fact]
        public async Task Handle_throws_on_null_request() =>
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await this._handler.Handle(null, CancellationToken.None));
    }
}
