namespace MovementPass.Public.Api.BackgroundJob.Infrastructure
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;

    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;

    public interface ITokenValidator
    {
        string Validate(string token);
    }

    public class TokenValidator : ITokenValidator
    {
        private readonly TokenValidationParameters _parameters;

        public TokenValidator(IOptions<JwtOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this._parameters = new TokenValidationParameters
            {
                ValidIssuer = options.Value.Issuer,
                ValidAudiences = new[] { options.Value.Audience },
                IssuerSigningKey = options.Value.Key(),
                ValidateIssuerSigningKey = true
            };
        }

        public string Validate(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var principal =
                    handler.ValidateToken(token, this._parameters, out _);

                var idClaim =
                    principal.Claims.FirstOrDefault(c => c.Type == "id");

                return idClaim?.Value;
            }
            catch (SecurityTokenValidationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}