namespace MovementPass.Public.Api.Tests;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using MediatR;

using Moq;
using Xunit;

using Controllers;
using Features;
using Features.Login;
using Features.Register;
using Infrastructure;

public class IdentityControllerTests
{
    private const string MobilePhone = "01512345678";
    private static readonly DateTime DateOfBirth = Clock.Now().AddYears(-new Random().Next(19, 80));

    private readonly Mock<IMediator> _mockedMediator;
    private readonly IdentityController _controller;

    public IdentityControllerTests()
    {
        this._mockedMediator = new Mock<IMediator>();

        this._controller = new IdentityController(this._mockedMediator.Object);
    }

    [Fact]
    public void Constructor_throws_on_null_Mediator() =>
        Assert.Throws<ArgumentNullException>(() => new IdentityController(null));

    [Fact]
    public async Task Login_successes_on_valid_credentials()
    {
        this._mockedMediator.Setup(m =>
                m.Send(
                    It.IsAny<LoginRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JwtResult
            {
                Type = "Bearer",
                Token = string.Join(string.Empty, Enumerable.Repeat("x", 128))
            });

        var result = await this._controller.Login(new LoginRequest
            {
                MobilePhone = MobilePhone,
                DateOfBirth = DateOfBirth.ToString("ddMMyyyy")
            }, CancellationToken.None)
            .ConfigureAwait(false) as OkObjectResult;

        Assert.NotNull(result);
        Assert.IsType<JwtResult>(result.Value);
    }

    [Fact]
    public async Task Login_fails_on_invalid_credentials()
    {
        this._mockedMediator.Setup(m =>
                m.Send(
                    It.IsAny<LoginRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((JwtResult)null);

        var result = await this._controller.Login(new LoginRequest
            {
                MobilePhone = MobilePhone,
                DateOfBirth = "15121971"
            }, CancellationToken.None)
            .ConfigureAwait(false) as BadRequestObjectResult;

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Register_successes_on_valid_input()
    {
        this._mockedMediator.Setup(m =>
                m.Send(
                    It.IsAny<RegisterRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JwtResult
            {
                Type = "Bearer",
                Token = string.Join(string.Empty, Enumerable.Repeat("x", 128))
            });

        var result = await this._controller.Register(
                new RegisterRequest(),
                CancellationToken.None)
            .ConfigureAwait(false) as ObjectResult;

        Assert.NotNull(result);
        Assert.IsType<JwtResult>(result.Value);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
    }

    [Fact]
    public async Task Register_fails_when_same_applicant_already_exists()
    {
        this._mockedMediator.Setup(m =>
                m.Send(
                    It.IsAny<RegisterRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync((JwtResult)null);

        var result = await this._controller.Register(
            new RegisterRequest(),
            CancellationToken.None) as BadRequestObjectResult;

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Photo_returns_valid_response()
    {
        this._mockedMediator.Setup(m =>
                m.Send(
                    It.IsAny<PhotoUrlRequest>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PhotoUrlResult());

        var result = await this._controller.Photo(new PhotoUrlRequest(), CancellationToken.None)
            .ConfigureAwait(false) as OkObjectResult;

        Assert.NotNull(result);
        Assert.IsType<PhotoUrlResult>(result.Value);
    }
}
