namespace MovementPass.Public.Api.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Options;

    using Amazon.S3;
    using Amazon.S3.Model;

    using Moq;
    using Xunit;

    using Features.Register;
    using Infrastructure;

    public class PhotoUrlHandlerTests
    {
        private readonly Mock<IAmazonS3> _mockedS3;

        private readonly PhotoUrlHandler _handler;

        public PhotoUrlHandlerTests()
        {
            var photoBucketOptions = new PhotoBucketOptions
            {
                Name = "photos.movement-pass.com",
                UploadExpiration = TimeSpan.FromMinutes(5)
            };

            this._mockedS3 = new Mock<IAmazonS3>();
            this._handler = new PhotoUrlHandler(this._mockedS3.Object,
                new OptionsWrapper<PhotoBucketOptions>(photoBucketOptions));
        }

        [Fact]
        public void Constructor_throws_on_null_S3() =>
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoUrlHandler(null, new Mock<IOptions<PhotoBucketOptions>>().Object));

        [Fact]
        public void Constructor_throws_on_null_PhotoBucketOptions() =>
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoUrlHandler(this._mockedS3.Object, null));

        [Fact]
        public async Task Handle_returns_valid_result()
        {
            const string filename = "4712362b634b4374a95d01a743878558.png";
            const string photoUrl = "https://s3.ap-south-1.amazonaws.com/photos.movement-pass.com/" + filename;

            this._mockedS3.Setup(s3 => s3.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
                .Returns(photoUrl);

            var result = await this._handler
                .Handle(new PhotoUrlRequest
                {
                    ContentType = "image/png",
                    Filename = "my_photo.png"
                }, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Handle_throws_on_null_request() =>
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await this._handler.Handle(null, CancellationToken.None));
    }
}
