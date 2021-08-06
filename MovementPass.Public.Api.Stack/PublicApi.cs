namespace MovementPass.Public.Api.Stack
{
    using System.Collections.Generic;

    using Amazon.CDK;
    using Amazon.CDK.AWS.APIGatewayv2;
    using Amazon.CDK.AWS.APIGatewayv2.Integrations;
    using Amazon.CDK.AWS.CertificateManager;
    using Amazon.CDK.AWS.DynamoDB;
    using Amazon.CDK.AWS.IAM;
    using Amazon.CDK.AWS.Lambda;
    using Amazon.CDK.AWS.Route53;
    using Amazon.CDK.AWS.Route53.Targets;
    using Amazon.CDK.AWS.S3;
    using Amazon.CDK.AWS.SSM;

    public sealed class PublicApi : Stack
    {
        public PublicApi(
            Construct scope,
            string id,
            IStackProps props = null) : base(scope, id, props)
        {
            const string subDomain = "public-api";

            var app = (string)this.Node.TryGetContext("app");
            var version = (string)this.Node.TryGetContext("version");

            var configRoot = $"/{app}/{version}";
            var name = $"{app}_{subDomain}_{version}";

            var lambda = new Function(this, "Lambda",
                new FunctionProps {
                    FunctionName = name,
                    Handler =
                        "MovementPass.Public.Api:MovementPass.Public.Api.LambdaEntryPoint::FunctionHandlerAsync",
                    Runtime = Runtime.DOTNET_CORE_3_1,
                    Timeout = Duration.Seconds(30),
                    MemorySize = 3008,
                    Code = Code.FromAsset($"dist/{name}.zip"),
                    Tracing = Tracing.ACTIVE,
                    Environment = new Dictionary<string, string>
                    {
                        { "ASPNETCORE_ENVIRONMENT", "Production" },
                        { "CONFIG_ROOT_KEY", configRoot }
                    }
                });

            lambda.AddToRolePolicy(new PolicyStatement(
                new PolicyStatementProps {
                    Effect = Effect.ALLOW,
                    Actions = new[] { "ssm:GetParametersByPath" },
                    Resources = new[]
                    {
                        $"arn:aws:ssm:{this.Region}:{this.Account}:parameter{configRoot}"
                    }
                }));

            foreach (var partialName in new[] { "applicants", "passes" })
            {
                var tableName = StringParameter.ValueForStringParameter(
                    this,
                    $"{configRoot}/dynamodbTables/{partialName}");

                var table = Table.FromTableArn(
                    this,
                    $"{partialName}Table",
                    $"arn:aws:dynamodb:{this.Region}:{this.Account}:table/{tableName}");

                table.GrantReadWriteData(lambda);

                lambda.AddToRolePolicy(
                    new PolicyStatement(
                        new PolicyStatementProps
                        {
                            Effect = Effect.ALLOW,
                            Actions = new[] { "dynamodb:Query" },
                            Resources = new[] { $"{table.TableArn}/index/*" }
                        }));
            }

            var photoBucketName = StringParameter.ValueForStringParameter(
                this,
                $"{configRoot}/photoBucket/name");

            var photoBucket = Bucket.FromBucketName(
                this,
                "PhotoBucket",
                photoBucketName);

            photoBucket.GrantPut(lambda);

            var integration = new LambdaProxyIntegration(
                new LambdaProxyIntegrationProps
                {
                    Handler = lambda,
                    PayloadFormatVersion = PayloadFormatVersion.VERSION_2_0
                });

            var api = new HttpApi(this, "Api", new HttpApiProps
            {
                ApiName = name,
                DefaultIntegration = integration,
                CorsPreflight = new CorsPreflightOptions
                {
                    AllowOrigins = new []{"*"},
                    AllowMethods = new []
                    {
                        CorsHttpMethod.GET,
                        CorsHttpMethod.POST
                    },
                    AllowHeaders = new []{"Authorization", "Content-Type"},
                    MaxAge = Duration.Days(365)
                },
                DisableExecuteApiEndpoint = true,
                CreateDefaultStage = false
            });

            var rootDomain = (string)this.Node.TryGetContext("domain");

            var certificateArn = StringParameter.ValueForStringParameter(
                    this,
                    $"{configRoot}/serverCertificateArn");

            var certificate = Certificate.FromCertificateArn(
                    this,
                    "CertificateArn",
                    certificateArn);

            var domainName = new DomainName(
                this,
                "Domain",
                new DomainNameProps
                {
                    DomainName = $"{subDomain}.{rootDomain}",
                    Certificate = certificate
                });

            api.AddStage("Stage", new HttpStageOptions
            {
                StageName = version,
                AutoDeploy = true,
                DomainMapping = new DomainMappingOptions
                {
                    DomainName = domainName,
                    MappingKey = version
                }
            });

            var zone = HostedZone.FromLookup(
                this,
                "Zone",
                new HostedZoneProviderProps
                {
                    DomainName = rootDomain
                });

            // ReSharper disable once ObjectCreationAsStatement
            new ARecord(
                this,
                "Mount",
                new ARecordProps
                {
                    RecordName = subDomain,
                    Target = RecordTarget.FromAlias(
                        new ApiGatewayv2DomainProperties(
                            domainName.RegionalDomainName,
                            domainName.RegionalHostedZoneId)),
                    Zone = zone
                });
        }
    }
}