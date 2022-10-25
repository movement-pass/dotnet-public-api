namespace MovementPass.Public.Api.Features.Login;

using System.ComponentModel.DataAnnotations;

using MediatR;

public class LoginRequest : IRequest<JwtResult>
{
    [Required, RegularExpression("^01[3-9]\\d{8}$")]
    public string MobilePhone { get; set; }

    [Required, RegularExpression("^\\d{8}$")]
    public string DateOfBirth { get; set; }
}