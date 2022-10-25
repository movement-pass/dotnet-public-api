namespace MovementPass.Public.Api.Infrastructure;

using System;
using System.Text;

using Microsoft.IdentityModel.Tokens;

public class JwtOptions
{
    public string Audience { get; set; }

    public string Issuer { get; set; }

    public string Secret { get; set; }

    public TimeSpan Expiration { get; set; }

    public SecurityKey Key() =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.Secret));

    public SigningCredentials Credentials() =>
        new SigningCredentials(this.Key(), SecurityAlgorithms.HmacSha256);
}