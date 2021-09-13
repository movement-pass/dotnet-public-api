namespace MovementPass.Public.Api.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;

    using MediatR;

    using Moq;
    using Xunit;

    using Controllers;

    using Features.ViewPass;
    using Features.ViewPasses;
    using Infrastructure;

    public class PassesControllerTests
    {
        private readonly Mock<IMediator> _mockedMediator;
        private readonly PassesController _controller;

        public PassesControllerTests()
        {
            this._mockedMediator = new Mock<IMediator>();
            this._controller = new PassesController(this._mockedMediator.Object);
        }

        [Fact]
        public void Constructor_throws_on_null_Mediator() =>
            Assert.Throws<ArgumentNullException>(() => new PassesController(null));

        [Fact]
        public async Task List_returns_matching_passes()
        {
            this._mockedMediator.Setup(
                    m => m.Send(
                        It.IsAny<ViewPassesRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PassListResult());

            var result = await this._controller.List(new PassListKey(), CancellationToken.None)
                .ConfigureAwait(false) as OkObjectResult;

            Assert.NotNull(result);
            Assert.IsType<PassListResult>(result.Value);
        }

        [Fact]
        public async Task Get_returns_matching_pass()
        {
            this._mockedMediator.Setup(
                    m => m.Send(
                        It.IsAny<ViewPassRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PassDetailItem());

            var result = await this._controller.Get(IdGenerator.Generate(), CancellationToken.None)
                .ConfigureAwait(false) as OkObjectResult;

            Assert.NotNull(result);
            Assert.IsType<PassDetailItem>(result.Value);
        }

        [Fact]
        public async Task Get_returns_not_found_if_pass_does_not_exist()
        {
            this._mockedMediator.Setup(
                m => m.Send(
                        It.IsAny<ViewPassRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync((PassDetailItem)null);

            var result = await this._controller.Get(IdGenerator.Generate(), CancellationToken.None)
                .ConfigureAwait(false) as NotFoundResult;

            Assert.NotNull(result);
        }
    }
}
