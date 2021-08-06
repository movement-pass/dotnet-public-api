namespace MovementPass.Public.Api.Features.Register
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Options;

    using Amazon.S3;
    using Amazon.S3.Model;

    using MediatR;

    using Infrastructure;

    public class PhotoUrlHandler :
        IRequestHandler<PhotoUrlRequest, PhotoUrlResult>
    {
        private readonly IAmazonS3 _s3;
        private readonly PhotoBucketOptions _photoBucketOptions;

        public PhotoUrlHandler(
            IAmazonS3 s3,
            IOptions<PhotoBucketOptions> photoBucketOptions)
        {
            if (photoBucketOptions == null)
            {
                throw new ArgumentNullException(nameof(photoBucketOptions));
            }

            this._s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
            this._photoBucketOptions = photoBucketOptions.Value;
        }

        public Task<PhotoUrlResult> Handle(
            PhotoUrlRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var filename = IdGenerator.Generate() +
                           Path.GetExtension(request.Filename);

            var req = new GetPreSignedUrlRequest
            {
                BucketName = this._photoBucketOptions.Name,
                Key = filename,
                ContentType = request.ContentType,
                Verb = HttpVerb.PUT,
                Expires = Clock.Now()
                    .Add(this._photoBucketOptions.UploadExpiration)
            };

            var url = this._s3.GetPreSignedURL(req);

            var result = new PhotoUrlResult { Url = url, Filename = filename };

            return Task.FromResult(result);
        }
    }
}