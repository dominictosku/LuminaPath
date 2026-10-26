using System.Net;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LuminaPath.Tests.Services.Storage;

/// <summary>Exercise the real SDK serializer/signing/stream handling without network or AWS credentials.</summary>
public sealed class S3SdkTransportTests
{
    [Fact]
    public async Task SignedSdkRequests_TransmitExactBytesAndConditionalHeaders()
    {
        using var handler = new S3Handler();
        using var client = new AmazonS3Client(new BasicAWSCredentials("test-key", "test-secret"), new AmazonS3Config
        {
            ServiceURL = "https://s3.example", AuthenticationRegion = "us-east-1", ForcePathStyle = true,
            HttpClientFactory = new TestHttpClientFactory(handler)
        });
        var storage = new S3Storage(client, Options.Create(new S3Options
        { BucketName = "bucket", ServiceUrl = "https://s3.example", ForcePathStyle = true }), NullLogger<S3Storage>.Instance);
        using var input = new MemoryStream([9, 1, 2, 3]);
        input.Position = 1;
        Assert.False((await storage.UploadExactAsync(input, "small name.bin")).Error);
        Assert.Equal(new byte[] { 1, 2, 3 }, Assert.Single(handler.SmallBodies));
        Assert.True(input.CanRead);

        var large = new byte[8 * 1024 * 1024 + 7];
        new Random(43).NextBytes(large);
        Assert.False((await storage.UploadExactAsync(new MemoryStream(large), "large.bin")).Error);
        Assert.Equal(2, handler.Parts.Count);
        Assert.Equal(large, handler.Parts.SelectMany(bytes => bytes).ToArray());
        Assert.True(handler.Completed);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : Amazon.Runtime.HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig) => new(handler, disposeHandler: false);
    }

    private sealed class S3Handler : HttpMessageHandler
    {
        public List<byte[]> SmallBodies { get; } = [];
        public List<byte[]> Parts { get; } = [];
        public bool Completed { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Assert.Equal("s3.example", request.RequestUri!.Host);
            Assert.StartsWith("AWS4-HMAC-SHA256 ", request.Headers.GetValues("Authorization").Single());
            Assert.False(request.Headers.Contains("x-amz-acl"));
            var query = request.RequestUri.Query;
            if (request.Method == HttpMethod.Put)
            {
                var bytes = await request.Content!.ReadAsByteArrayAsync(ct);
                if (request.Content.Headers.ContentEncoding.Contains("aws-chunked")) bytes = DecodeChunks(bytes);
                if (query.Contains("partNumber=")) Parts.Add(bytes);
                else
                {
                    Assert.Equal("*", request.Headers.GetValues("If-None-Match").Single());
                    Assert.Equal("/bucket/luminapath/uploads/small%20name.bin", request.RequestUri.AbsolutePath);
                    SmallBodies.Add(bytes);
                }
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
                response.Headers.TryAddWithoutValidation("ETag", "\"test-etag\"");
                return response;
            }
            if (request.Method == HttpMethod.Post && query.Contains("uploads"))
                return Xml("<InitiateMultipartUploadResult><Bucket>bucket</Bucket><Key>luminapath/uploads/large.bin</Key><UploadId>upload-id</UploadId></InitiateMultipartUploadResult>");
            if (request.Method == HttpMethod.Post && query.Contains("uploadId="))
            {
                Assert.Equal("*", request.Headers.GetValues("If-None-Match").Single());
                Completed = true;
                return Xml("<CompleteMultipartUploadResult><Bucket>bucket</Bucket><Key>luminapath/uploads/large.bin</Key><ETag>\"test-etag\"</ETag></CompleteMultipartUploadResult>");
            }
            throw new InvalidOperationException("Unexpected SDK request.");
        }
        private static HttpResponseMessage Xml(string value) => new(HttpStatusCode.OK)
        { Content = new StringContent(value, Encoding.UTF8, "application/xml") };
        private static byte[] DecodeChunks(byte[] input)
        {
            using var output = new MemoryStream();
            var offset = 0;
            while (offset < input.Length)
            {
                var end = offset;
                while (input[end] != '\r' || input[end + 1] != '\n') end++;
                var line = Encoding.ASCII.GetString(input, offset, end - offset).Split(';')[0];
                var size = Convert.ToInt32(line, 16);
                if (size == 0) break;
                offset = end + 2;
                output.Write(input, offset, size);
                offset += size + 2;
            }
            return output.ToArray();
        }
    }
}
