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
    using Features.Apply;
    using Features.ViewPass;
    using Features.ViewPasses;
    using Infrastructure;
    using Microsoft.AspNetCore.Http;

    public class PassesControllerTests
    {
        private readonly Mock<IMediator> _mockedMediator;
        private readonly PassesController _controller;

        public PassesControllerTests()
        {
            this._mockedMediator = new Mock<IMediator>();

            this._controller = new PassesController(this._mockedMediator.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public void Constructor_throws_on_null_Mediator() =>
            Assert.Throws<ArgumentNullException>(() => new PassesController(null));

        [Fact]
        public async Task Apply_successes_on_valid_input()
        {
            this._mockedMediator.Setup(
                    m => m.Send(
                        It.IsAny<ApplyRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IdResult { Id = IdGenerator.Generate() });

            var result = await this._controller.Apply(new ApplyRequest(), CancellationToken.None)
                .ConfigureAwait(false) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.IsType<IdResult>(result.Value);
        }

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
