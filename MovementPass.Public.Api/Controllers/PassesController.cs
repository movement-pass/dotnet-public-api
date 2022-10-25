namespace MovementPass.Public.Api.Controllers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;

using MediatR;

using Features.Apply;
using Features.ViewPass;
using Features.ViewPasses;

[Authorize]
[Route("[controller]")]
public class PassesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PassesController(IMediator mediator) =>
        this._mediator = mediator ??
                         throw new ArgumentNullException(nameof(mediator));

    [HttpPost("")]
    [ProducesResponseType(typeof(IdResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<string>>),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Apply(
        [FromBody, BindRequired] ApplyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this._mediator
            .Send(request, cancellationToken)
            .ConfigureAwait(true);

        return this.CreatedAtAction(
            nameof(this.Get),
            result,
            result);
    }

    [HttpGet("")]
    [ResponseCache(Duration = 180, Location = ResponseCacheLocation.Client)]
    [ProducesResponseType(typeof(PassListResult), StatusCodes.Status200OK)]
    public async Task<ActionResult> List([FromQuery] PassListKey startKey,
        CancellationToken cancellationToken)
    {
        var result = await this._mediator
            .Send(new ViewPassesRequest { StartKey = startKey },
                cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(result);
    }

    [HttpGet("{id:length(32)}")]
    [ProducesResponseType(typeof(PassDetailItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Get(
        [FromRoute, BindRequired] string id,
        CancellationToken cancellationToken)
    {
        var pass = await this._mediator
            .Send(new ViewPassRequest { Id = id }, cancellationToken)
            .ConfigureAwait(false);

        if (pass == null)
        {
            return this.NotFound();
        }

        var headers = this.Response.GetTypedHeaders();
            
        headers.CacheControl =
            new CacheControlHeaderValue {
                Private = true,
                MaxAge =
                    string.Equals(pass.Status, "APPLIED",
                        StringComparison.OrdinalIgnoreCase)
                        ? TimeSpan.FromMinutes(3)
                        : TimeSpan.FromDays(30)
            };

        return this.Ok(pass);
    }
}