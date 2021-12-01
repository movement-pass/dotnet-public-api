namespace MovementPass.Public.Api.Features.ViewPass
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.Model;
    using MediatR;
    using Entities;
    using ExtensionMethods;
    using Infrastructure;

    public class ViewPassHandler :
        IRequestHandler<ViewPassRequest, PassDetailItem>
    {
        private readonly IAmazonDynamoDB _dynamodb;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly DynamoDBTablesOptions _tableOptions;

        public ViewPassHandler(
            IAmazonDynamoDB dynamodb,
            ICurrentUserProvider currentUserProvider,
            IOptions<DynamoDBTablesOptions> tableOptions)
        {
            if (tableOptions == null)
            {
                throw new ArgumentNullException(nameof(tableOptions));
            }

            this._dynamodb = dynamodb ??
                             throw new ArgumentNullException(nameof(dynamodb));

            this._currentUserProvider = currentUserProvider ??
                                        throw new ArgumentNullException(
                                            nameof(currentUserProvider));
            this._tableOptions = tableOptions.Value;
        }

        public async Task<PassDetailItem> Handle(
            ViewPassRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var pass = await this.GetPass(request.Id, cancellationToken)
                .ConfigureAwait(false);

            if (pass == null)
            {
                return null;
            }

            var applicant = await this
                .GetApplicant(pass.ApplicantId, cancellationToken)
                .ConfigureAwait(false);

            var detail = new PassDetailItem {
                Applicant = new ApplicantItem().Merge(applicant)
            }.Merge(pass);

            return detail;
        }

        private async Task<Pass> GetPass(
            string id,
            CancellationToken cancellationToken)
        {
            var req = new GetItemRequest {
                TableName = this._tableOptions.Passes,
                Key = new Dictionary<string, AttributeValue> {
                    { "id", new AttributeValue { S = id } }
                }
            };

            var res = await this._dynamodb.GetItemAsync(
                    req,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!res.Item.Any() ||
                !res.Item.ContainsKey("applicantId") ||
                !string.Equals(res.Item["applicantId"].S,
                    this._currentUserProvider.UserId, StringComparison.Ordinal))
            {
                return null;
            }

            return res.Item.FromDynamoDBAttributes<Pass>();
        }

        private async Task<Applicant> GetApplicant(
            string id,
            CancellationToken cancellationToken)
        {
            var req = new GetItemRequest {
                TableName = this._tableOptions.Applicants,
                Key = new Dictionary<string, AttributeValue> {
                    { "id", new AttributeValue { S = id } }
                }
            };

            var res = await this._dynamodb.GetItemAsync(
                    req,
                    cancellationToken)
                .ConfigureAwait(false);

            return res.Item.Any()
                ? res.Item.FromDynamoDBAttributes<Applicant>()
                : null;
        }
    }
}