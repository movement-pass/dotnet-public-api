namespace MovementPass.Public.Api.Infrastructure
{
    using System;

    public class PhotoBucketOptions
    {
        public string Name { get; set; }

        public TimeSpan UploadExpiration { get; set; }
    }
}