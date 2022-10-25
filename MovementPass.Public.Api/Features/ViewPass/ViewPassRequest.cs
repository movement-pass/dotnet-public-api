namespace MovementPass.Public.Api.Features.ViewPass;

using MediatR;

public class ViewPassRequest : IRequest<PassDetailItem>
{
    public string Id { get; set; }
}