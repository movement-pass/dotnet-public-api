namespace MovementPass.Public.Api.Features.ViewPasses;

using MediatR;

public class ViewPassesRequest : IRequest<PassListResult>
{
    public PassListKey StartKey { get; set; }
}