namespace MovementPass.Public.Api.ExtensionMethods;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Entities;
using Features;
using Infrastructure;

public static class ApplicantExtensions
{
    public static JwtResult GenerateJwt(
        this Applicant instance,
        JwtOptions options)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var claims = new[]
        {
            new Claim("id", instance.Id),
            new Claim("name", instance.Name),
            new Claim("photo", instance.Photo)
        };

        var now = Clock.Now();
        var expires = now.Add(options.Expiration);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = options.Credentials()
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);

        var result = new JwtResult
        {
            Type = JwtBearerDefaults.AuthenticationScheme,
            Token = handler.WriteToken(token)
        };

        return result;
    }
}