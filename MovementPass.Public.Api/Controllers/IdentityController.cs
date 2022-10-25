namespace MovementPass.Public.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using MediatR;

using ExtensionMethods;
using Features;
using Features.Login;
using Features.Register;

[Route("[controller]/[action]")]
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;

    public IdentityController(IMediator mediator) =>
        this._mediator = mediator ??
                         throw new ArgumentNullException(nameof(mediator));

    [HttpPost]
    [ProducesResponseType(typeof(JwtResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<string>>),
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody, BindRequired] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result= await this._mediator
            .Send(request, cancellationToken)
            .ConfigureAwait(false);

        return result == null ?
            (ActionResult)this.ClientError("Invalid credential!") :
            this.Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(JwtResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<string>>),
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody, BindRequired] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this._mediator
            .Send(request, cancellationToken)
            .ConfigureAwait(false);

        if (result == null)
        {
            return this.ClientError("Mobile phone is already registered!");
        }

        return new ObjectResult(result)
        {
            StatusCode = StatusCodes.Status201Created
        };
    }

    [HttpPost]
    [ProducesResponseType(typeof(PhotoUrlResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<string>>),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Photo(PhotoUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this._mediator.Send(request, cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(result);
    }
}