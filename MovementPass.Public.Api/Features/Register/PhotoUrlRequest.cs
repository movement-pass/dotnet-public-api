namespace MovementPass.Public.Api.Features.Register;

using System.ComponentModel.DataAnnotations;

using MediatR;

public class PhotoUrlRequest : IRequest<PhotoUrlResult>
{
    [Required, RegularExpression("^image\\/(png|jpg|jpeg)$")]
    public string ContentType { get; set; }

    [Required, FileExtensions(Extensions = "png,jpg,jpeg")]
    public string Filename { get; set; }
}