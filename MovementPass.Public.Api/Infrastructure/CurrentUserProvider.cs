namespace MovementPass.Public.Api.Infrastructure;

using System;
using System.Security.Claims;

using Microsoft.AspNetCore.Http;

public interface ICurrentUserProvider
{
    string UserId { get; }
}

public class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor) =>
        this._httpContextAccessor = httpContextAccessor ??
                                    throw new ArgumentNullException(
                                        nameof(httpContextAccessor));

    public string UserId
    {
        get
        {
            var id = this._httpContextAccessor.HttpContext!.User
                .FindFirstValue("id");

            return id;
        }
    }
}