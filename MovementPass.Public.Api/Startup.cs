[assembly: Microsoft.AspNetCore.Mvc.ApiController]

namespace MovementPass.Public.Api;

using System;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;

using ExtensionMethods;
using Infrastructure;

public class Startup
{
    private const string ApiTitle = "Movement Pass Public API";
    private const string DocName = "docs";

    public Startup(IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        this.Configuration = configuration ??
                             throw new ArgumentNullException(
                                 nameof(configuration));
        this.HostEnvironment = environment ??
                               throw new ArgumentNullException(
                                   nameof(environment));
    }

    private IConfiguration Configuration { get; }

    private IWebHostEnvironment HostEnvironment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddCors();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var config = this.Configuration.Load<JwtOptions>();

                options.TokenValidationParameters =
                    new TokenValidationParameters {
                        ValidAudience = config.Audience,
                        ValidIssuer = config.Issuer,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        IssuerSigningKey = config.Key()
                    };
                options.RequireHttpsMetadata =
                    this.HostEnvironment.IsProduction();
            });

        services.AddRouting(options => options.LowercaseUrls = true);

        services.AddControllers(options =>
            {
                static void Apply(JsonSerializerOptions settings)
                {
                    settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    settings.DictionaryKeyPolicy =
                        JsonNamingPolicy.CamelCase;
                }

                var input = options.InputFormatters
                    .OfType<SystemTextJsonInputFormatter>().First();
                var output = options.OutputFormatters
                    .OfType<SystemTextJsonOutputFormatter>().First();

                Apply(input.SerializerOptions);
                Apply(output.SerializerOptions);

                options.Filters.Add(
                    new ProducesAttribute(MediaTypeNames.Application.Json));
                options.Filters.Add(new ProducesResponseTypeAttribute(
                    StatusCodes.Status500InternalServerError));
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .SelectMany(ms =>
                            ms.Value?.Errors.Select(e =>
                                e.Exception?.Message ?? e.ErrorMessage))
                        .ToList();

                    return new BadRequestObjectResult(new { errors });
                };
            });

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocName,
                new OpenApiInfo {Title = ApiTitle, Version = "v1"});

            options.AddSecurityDefinition("bearer",
                new OpenApiSecurityScheme {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "JWT Authorization header using the Bearer scheme."
                });

            options.OperationFilter<AuthorizeOperationFilter>();
        });

        services.AddDefaultAWSOptions(this.Configuration.GetAWSOptions());
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonS3>();

        var production =
            Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT") != null;

        if (production)
        {
            services.AddTransient<IAWSXRayRecorder>(_ =>
                AWSXRayRecorder.Instance);

            AWSXRayRecorder.InitializeInstance(this.Configuration);
            AWSSDKHandler.RegisterXRayForAllServices();
        }
        else
        {
            services.AddTransient<IAWSXRayRecorder, StubbedXray>();
        }

        services.AddLogging(options => options.AddLambdaLogger(
            this.Configuration, "logging"));

        services.AddMediatR(options => options.RegisterServicesFromAssembly(this.GetType().Assembly));

        this.Configuration.Apply<DynamoDBTablesOptions>(services);
        this.Configuration.Apply<PhotoBucketOptions>(services);
        this.Configuration.Apply<JwtOptions>(services);

        services.AddTransient<ICurrentUserProvider, CurrentUserProvider>();
    }

    public void Configure(IApplicationBuilder app)
    {
        if (this.HostEnvironment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors(x =>
            x.AllowAnyOrigin()
                .WithHeaders("Authorization", "Content-Type")
                .WithMethods("GET", "POST")
                .SetPreflightMaxAge(TimeSpan.FromDays(365)));

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(options => options.MapControllers());

        var swaggerPath =
            (this.HostEnvironment.IsDevelopment() ? "/" : string.Empty) +
            $"{DocName}/swagger.json";

        app.UseSwagger(options =>
                options.RouteTemplate = "{documentName}/swagger.json")
            .UseSwaggerUI(options =>
            {
                options.DocumentTitle = ApiTitle;
                options.RoutePrefix = string.Empty;
                options.SwaggerEndpoint(swaggerPath, ApiTitle);
            });
    }
}