namespace MovementPass.Public.Api.ExtensionMethods;

using System;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

public static class ControllerExtensions
{
    public static IActionResult ClientError(
        this ControllerBase instance,
        string errorMessage)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        instance.ModelState.AddModelError(string.Empty, errorMessage);

        var errors = instance.ModelState
            .SelectMany(ms =>
                ms.Value?.Errors.Select(e =>
                    e.Exception?.Message ?? e.ErrorMessage))
            .ToList();

        return instance.BadRequest(new {errors});
    }
}