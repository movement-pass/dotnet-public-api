namespace MovementPass.Public.Api.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MediatR;

using NSubstitute;
using Xunit;

using Controllers;
using Features.Apply;
using Features.ViewPass;
using Features.ViewPasses;
using Infrastructure;

public class PassesControllerTests
{
    private readonly IMediator _mockedMediator;
    private readonly PassesController _controller;

    public PassesControllerTests()
    {
        this._mockedMediator = Substitute.For<IMediator>();

        this._controller = new PassesController(this._mockedMediator)
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
        this._mockedMediator.Send(Arg.Any<ApplyRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IdResult
            {
                Id = IdGenerator.Generate()
            }));
        
        var result = await this._controller.Apply(
                new ApplyRequest(),
                CancellationToken.None)
            as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.IsType<IdResult>(result.Value);
    }

    [Fact]
    public async Task List_returns_matching_passes()
    {
        this._mockedMediator.Send(Arg.Any<ViewPassesRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PassListResult()));
        
        var result = await this._controller.List(
                new PassListKey(),
                CancellationToken.None)
            as OkObjectResult;

        Assert.NotNull(result);
        Assert.IsType<PassListResult>(result.Value);
    }

    [Fact]
    public async Task Get_returns_matching_pass()
    {
        this._mockedMediator.Send(Arg.Any<ViewPassRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PassDetailItem()));
        
        var result = await this._controller.Get(
            IdGenerator.Generate(),
            CancellationToken.None) as OkObjectResult;

        Assert.NotNull(result);
        Assert.IsType<PassDetailItem>(result.Value);
    }

    [Fact]
    public async Task Get_returns_not_found_if_pass_does_not_exist()
    {
        this._mockedMediator.Send(Arg.Any<ViewPassRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<PassDetailItem>(null));
        
        var result = await this._controller.Get(
            IdGenerator.Generate(),
            CancellationToken.None) as NotFoundResult;

        Assert.NotNull(result);
    }
}
