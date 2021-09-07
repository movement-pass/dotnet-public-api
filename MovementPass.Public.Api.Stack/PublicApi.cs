namespace MovementPass.Public.Api.Stack
{
    using System.Collections.Generic;

    using Amazon.CDK;
    using Amazon.CDK.AWS.APIGateway;
    using Amazon.CDK.AWS.CertificateManager;
    using Amazon.CDK.AWS.DynamoDB;
    using Amazon.CDK.AWS.IAM;
    using Amazon.CDK.AWS.Kinesis;
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
                new FunctionProps
                {
                    FunctionName = name,
                    Handler =
                        "MovementPass.Public.Api::MovementPass.Public.Api.LambdaEntryPoint::FunctionHandlerAsync",
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
                new PolicyStatementProps
                {
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

            var stream = Stream.FromStreamArn(this, "Stream", "");

            var role = new Role(this, "Role",
                new RoleProps
                {
                    AssumedBy = new ServicePrincipal("apigateway.amazonaws.com")
                });

            stream.GrantWrite(role);

            var lambdaIntegration = new LambdaIntegration(
                lambda,
                new LambdaIntegrationOptions { Proxy = true });

            var api = new RestApi(this, "Api",
                new RestApiProps
                {
                    RestApiName = name,
                    MinimumCompressionSize = 1024,
                    EndpointTypes = new[] { EndpointType.REGIONAL },
                    DefaultCorsPreflightOptions = new CorsOptions
                    {
                        AllowOrigins = new[] { "*" },
                        AllowMethods = new[] { "GET", "POST" },
                        AllowHeaders =
                            new[] { "Authorization", "Content-Type" },
                        MaxAge = Duration.Days(365)
                    },
                    DeployOptions = new StageOptions
                    {
                        MetricsEnabled = true,
                        TracingEnabled = true,
                        LoggingLevel = MethodLoggingLevel.ERROR,
                        StageName = version
                    },
                    CloudWatchRole = true,
                    DefaultIntegration = lambdaIntegration
                });

            var proxyResource = api.Root.AddProxy(new ProxyResourceOptions
            {
                AnyMethod = false
            });

            proxyResource.AddMethod("GET");
            proxyResource.AddMethod("POST");

            var kinesisIntegration = new AwsIntegration(
                new AwsIntegrationProps
                {
                    Service = "kinesis",
                    Action = "PutRecord",
                    IntegrationHttpMethod = "POST",
                    Options = new IntegrationOptions
                    {
                        PassthroughBehavior = PassthroughBehavior.NEVER,
                        ConnectionType = ConnectionType.INTERNET,
                        CredentialsRole = role,
                        RequestParameters =
                            new Dictionary<string, string> {
                                {
                                    "integration.request.header.Content-Type",
                                    "'application/json'"
                                }
                            },
                        RequestTemplates =
                            new Dictionary<string, string>
                            {
                                {
                                    "application/json",
                                    this.ToJsonString(new
                                    {
                                        stream.StreamName,
                                        Data = "$util.base64Encode($input.json('$'))",
                                        PartitionKey = "$input.params().header.get('authorization')"
                                    })
                                }
                            },
                        IntegrationResponses = new IIntegrationResponse[]
                        {
                            new IntegrationResponse
                            {
                                StatusCode = "200",
                                ResponseTemplates = new Dictionary<string, string>
                                {
                                    { "application/json", "Ok" }
                                },
                                SelectionPattern = "200"
                            },
                            new IntegrationResponse
                            {
                                StatusCode = "500",
                                ResponseTemplates =
                                    new Dictionary<string, string>
                                    {
                                        { "application/json", "Error" }
                                    },
                                SelectionPattern = "500"
                            }
                        }
                    }
                });

            api.Root.AddResource("passes").AddMethod("POST", kinesisIntegration,
                new MethodOptions {
                    MethodResponses = new IMethodResponse[]
                    {
                        new MethodResponse
                        {
                            StatusCode = "200",
                            ResponseParameters = new Dictionary<string, bool>
                            {
                                { "method.response.header.Content-Type", true }
                            }
                        },
                        new MethodResponse {
                            StatusCode = "500",
                            ResponseParameters = new Dictionary<string, bool>
                            {
                                { "method.response.header.Content-Type", true }
                            }
                        }
                    }
                });

            var rootDomain = (string)this.Node.TryGetContext("domain");

            var certificateArn = StringParameter.ValueForStringParameter(
                this,
                $"{configRoot}/serverCertificateArn");

            var certificate = Certificate.FromCertificateArn(
                this,
                "CertificateArn",
                certificateArn);

            var domainName = new DomainName_(this, "DomainName",
                new DomainNameProps
                {
                    DomainName = $"{subDomain}.{rootDomain}",
                    EndpointType = EndpointType.REGIONAL,
                    SecurityPolicy = SecurityPolicy.TLS_1_2,
                    Certificate = certificate
                });

            // ReSharper disable once ObjectCreationAsStatement
            new BasePathMapping(this, $"ApiMapping",
                new BasePathMappingProps
                {
                    RestApi = api,
                    DomainName = domainName,
                    BasePath = version
                });

            var domain = DomainName_.FromDomainNameAttributes(
                this,
                "Domain",
                new DomainNameAttributes
                {
                    DomainName = domainName.DomainName,
                    DomainNameAliasHostedZoneId = domainName.DomainNameAliasHostedZoneId,
                    DomainNameAliasTarget = domainName.DomainNameAliasDomainName
                });

            var zone = HostedZone.FromLookup(
                this,
                "Zone",
                new HostedZoneProviderProps
                {
                    DomainName = rootDomain
                });

            // ReSharper disable once ObjectCreationAsStatement
            new ARecord(this, "Mount",
                new ARecordProps
                {
                    RecordName = subDomain,
                    Target = RecordTarget.FromAlias(new ApiGatewayDomain(domain)),
                    Zone = zone
                });
        }
    }
}