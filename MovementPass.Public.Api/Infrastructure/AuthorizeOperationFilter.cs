namespace MovementPass.Public.Api.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.OpenApi.Models;

    using Swashbuckle.AspNetCore.SwaggerGen;

    public class AuthorizeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation,
            OperationFilterContext context)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var actionAttributes =
                context.MethodInfo.GetCustomAttributes(true).ToList();

            if (actionAttributes.OfType<IAllowAnonymous>().Any())
            {
                return;
            }

            if (!actionAttributes.OfType<AuthorizeAttribute>().Any())
            {
                var controllerAttributes = context.MethodInfo.DeclaringType?
                    .GetCustomAttributes(true)
                    .ToList() ?? Enumerable.Empty<object>().ToList();

                if (controllerAttributes.OfType<IAllowAnonymous>().Any())
                {
                    return;
                }

                if (!controllerAttributes.OfType<AuthorizeAttribute>().Any())
                {
                    var globalFilters = context.ApiDescription.ActionDescriptor
                        .FilterDescriptors.Select(d => d.Filter)
                        .ToList();

                    if (!globalFilters.Any(f => f is AuthorizeFilter))
                    {
                        return;
                    }
                }
            }

            var jwtBearerScheme = new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme, Id = "bearer"
                }
            };

            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    [jwtBearerScheme] = Array.Empty<string>()
                }
            };

            if (!operation.Responses.ContainsKey("401"))
            {
                operation.Responses.Add("401",
                    new OpenApiResponse {Description = "Unauthorized"});
            }
        }
    }
}