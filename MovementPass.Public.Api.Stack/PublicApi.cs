namespace MovementPass.Public.Api.Stack;

using System.Collections.Generic;
using System.Text.Json;

using Constructs;
using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Route53;
using Amazon.CDK.AWS.Route53.Targets;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SQS;

public sealed class PublicApi : BaseStack
{
    public PublicApi(
        Construct scope,
        string id,
        IStackProps props = null) : base(scope, id, props)
    {
        const string subDomain = "public-api";

        var name = $"{this.App}_{subDomain}_{this.Version}";

        var lambda = new Function(this, "Lambda",
            new FunctionProps {
                FunctionName = name,
                Handler =
                    "MovementPass.Public.Api::MovementPass.Public.Api.LambdaEntryPoint::FunctionHandlerAsync",
                Runtime = Runtime.DOTNET_6,
                Timeout = Duration.Seconds(30),
                MemorySize = 3008,
                Code = Code.FromAsset($"dist/{name}.zip"),
                Tracing = Tracing.ACTIVE,
                Environment = new Dictionary<string, string> {
                    { "ASPNETCORE_ENVIRONMENT", "Production" },
                    { "CONFIG_ROOT_KEY", this.ConfigRootKey }
                }
            });

        lambda.AddToRolePolicy(new PolicyStatement(
            new PolicyStatementProps {
                Effect = Effect.ALLOW,
                Actions = new[] { "ssm:GetParametersByPath" },
                Resources = new[] {
                    $"arn:aws:ssm:{this.Region}:{this.Account}:parameter{this.ConfigRootKey}"
                }
            }));

        foreach (var partialName in new[] { "applicants", "passes" })
        {
            var tableName =
                this.GetParameterStoreValue(
                    $"dynamodbTables/{partialName}");

            var table = Table.FromTableArn(
                this,
                $"{partialName}Table",
                $"arn:aws:dynamodb:{this.Region}:{this.Account}:table/{tableName}");

            table.GrantReadWriteData(lambda);

            lambda.AddToRolePolicy(
                new PolicyStatement(
                    new PolicyStatementProps {
                        Effect = Effect.ALLOW,
                        Actions = new[] { "dynamodb:Query" },
                        Resources = new[] { $"{table.TableArn}/index/*" }
                    }));
        }

        var photoBucketName =
            this.GetParameterStoreValue("photoBucket/name");

        var photoBucket = Bucket.FromBucketName(
            this,
            "PhotoBucket",
            photoBucketName);

        photoBucket.GrantPut(lambda);

        var queue = Queue.FromQueueArn(
            this,
            "Queue",
            $"arn:aws:sqs:{this.Region}:{this.Account}:{this.App}_passes_load_{this.Version}.fifo");

        var role = new Role(this, "Role",
            new RoleProps {
                AssumedBy = new ServicePrincipal("apigateway.amazonaws.com")
            });

        queue.GrantSendMessages(role);

        var lambdaIntegration = new LambdaIntegration(
            lambda,
            new LambdaIntegrationOptions { Proxy = true });

        var api = new RestApi(this, "Api",
            new RestApiProps {
                RestApiName = name,
                MinCompressionSize = Size.Bytes(1024),
                EndpointTypes = new[] { EndpointType.REGIONAL },
                DefaultCorsPreflightOptions = new CorsOptions {
                    AllowOrigins = new[] { "*" },
                    AllowMethods = new[] { "GET", "POST" },
                    AllowHeaders =
                        new[] { "Authorization", "Content-Type" },
                    MaxAge = Duration.Days(365)
                },
                DeployOptions = new StageOptions {
                    MetricsEnabled = true,
                    TracingEnabled = true,
                    LoggingLevel = MethodLoggingLevel.ERROR,
                    StageName = this.Version
                },
                CloudWatchRole = true,
                DefaultIntegration = lambdaIntegration
            });

        var proxyResource =
            api.Root.AddProxy(
                new ProxyResourceOptions { AnyMethod = false });

        proxyResource.AddMethod("GET");
        proxyResource.AddMethod("POST");

        var queueIntegration = new AwsIntegration(
            new AwsIntegrationProps {
                Service = "sqs",
                Path = queue.QueueName,
                IntegrationHttpMethod = "POST",
                Options = new IntegrationOptions {
                    PassthroughBehavior = PassthroughBehavior.NEVER,
                    ConnectionType = ConnectionType.INTERNET,
                    CredentialsRole = role,
                    RequestParameters =
                        new Dictionary<string, string> {
                            {
                                "integration.request.header.Content-Type",
                                "'application/x-www-form-urlencoded'"
                            }
                        },
                    RequestTemplates =
                        new Dictionary<string, string> {
                            {
                                "application/json",
                                "Action=SendMessage&MessageBody=$util.urlEncode(\"$input.body\")&MessageGroupId=$input.path(\"$.thana\")"
                            }
                        },
                    IntegrationResponses = new IIntegrationResponse[] {
                        new IntegrationResponse {
                            StatusCode = "200",
                            ResponseParameters =
                                new Dictionary<string, string> {
                                    {
                                        "method.response.header.Access-Control-Allow-Origin",
                                        "'*'"
                                    }
                                },
                            ResponseTemplates =
                                new Dictionary<string, string> {
                                    {
                                        "application/json",
                                        JsonSerializer.Serialize(new {
                                            success = true
                                        })
                                    }
                                },
                            SelectionPattern = "200"
                        },
                        new IntegrationResponse {
                            StatusCode = "500",
                            ResponseParameters =
                                new Dictionary<string, string> {
                                    {
                                        "method.response.header.Access-Control-Allow-Origin",
                                        "'*'"
                                    }
                                },
                            ResponseTemplates =
                                new Dictionary<string, string> {
                                    {
                                        "application/json",
                                        JsonSerializer.Serialize(new {
                                            error = "internal server error!"
                                        })
                                    }
                                },
                            SelectionPattern = "500"
                        }
                    }
                }
            });

        api.Root.AddResource("passes").AddMethod("POST", queueIntegration,
            new MethodOptions {
                MethodResponses = new IMethodResponse[] {
                    new MethodResponse {
                        StatusCode = "200",
                        ResponseParameters = new Dictionary<string, bool> {
                            { "method.response.header.Content-Type", true }, {
                                "method.response.header.Access-Control-Allow-Origin",
                                true
                            }
                        }
                    },
                    new MethodResponse {
                        StatusCode = "500",
                        ResponseParameters = new Dictionary<string, bool> {
                            { "method.response.header.Content-Type", true }, {
                                "method.response.header.Access-Control-Allow-Origin",
                                true
                            }
                        }
                    }
                }
            });

        var certificateArn =
            this.GetParameterStoreValue("serverCertificateArn");

        var certificate = Certificate.FromCertificateArn(
            this,
            "CertificateArn",
            certificateArn);

        var domainName = new DomainName_(this, "DomainName",
            new DomainNameProps {
                DomainName = $"{subDomain}.{this.Domain}",
                EndpointType = EndpointType.REGIONAL,
                SecurityPolicy = SecurityPolicy.TLS_1_2,
                Certificate = certificate
            });

        // ReSharper disable once ObjectCreationAsStatement
        new BasePathMapping(this, $"ApiMapping",
            new BasePathMappingProps {
                RestApi = api,
                DomainName = domainName,
                BasePath = this.Version
            });

        var domain = DomainName_.FromDomainNameAttributes(
            this,
            "Domain",
            new DomainNameAttributes {
                DomainName = domainName.DomainName,
                DomainNameAliasHostedZoneId =
                    domainName.DomainNameAliasHostedZoneId,
                DomainNameAliasTarget = domainName.DomainNameAliasDomainName
            });

        var zone = HostedZone.FromLookup(
            this,
            "Zone",
            new HostedZoneProviderProps { DomainName = this.Domain });

        // ReSharper disable once ObjectCreationAsStatement
        new ARecord(this, "Mount",
            new ARecordProps {
                RecordName = subDomain,
                Target =
                    RecordTarget.FromAlias(new ApiGatewayDomain(domain)),
                Zone = zone
            });
    }
}