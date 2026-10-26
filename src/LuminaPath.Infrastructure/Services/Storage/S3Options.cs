using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using LuminaPath.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace LuminaPath.Infrastructure.Services.Storage;

public sealed class S3Options
{
    public const string SectionName = "S3";
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string? ServiceUrl { get; set; }
    public bool ForcePathStyle { get; set; }
    public string Prefix { get; set; } = "luminapath/";
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? SessionToken { get; set; }
    public bool UploadBackups { get; set; } = true;

    internal static void ReadSecrets(S3Options options, IConfiguration config)
    {
        options.AccessKeyId = SecretConfiguration.GetSecret(config, "S3:AccessKeyId", "S3:AccessKeyIdFile");
        options.SecretAccessKey = SecretConfiguration.GetSecret(config, "S3:SecretAccessKey", "S3:SecretAccessKeyFile");
        options.SessionToken = SecretConfiguration.GetSecret(config, "S3:SessionToken", "S3:SessionTokenFile");
    }

    internal static bool IsValid(S3Options options) =>
        !string.IsNullOrWhiteSpace(options.BucketName) && !options.BucketName.Contains('/')
        && !options.BucketName.Any(char.IsWhiteSpace) && !string.IsNullOrWhiteSpace(options.Region)
        && (string.IsNullOrWhiteSpace(options.ServiceUrl)
            || Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out var uri)
            && uri.Scheme is "https" or "http" && string.IsNullOrEmpty(uri.UserInfo)
            && uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment))
        && string.IsNullOrWhiteSpace(options.AccessKeyId) == string.IsNullOrWhiteSpace(options.SecretAccessKey)
        && (string.IsNullOrWhiteSpace(options.SessionToken) || !string.IsNullOrWhiteSpace(options.AccessKeyId))
        && options.Prefix is not null && !options.Prefix.StartsWith('/') && !options.Prefix.Contains('\\')
        && !options.Prefix.Any(char.IsControl) && !options.Prefix.Split('/').Any(segment => segment is "." or "..");

    internal AmazonS3Config CreateClientConfig()
    {
        var config = new AmazonS3Config { ForcePathStyle = ForcePathStyle };
        if (string.IsNullOrWhiteSpace(ServiceUrl)) config.RegionEndpoint = RegionEndpoint.GetBySystemName(Region);
        else
        {
            config.ServiceURL = ServiceUrl;
            config.AuthenticationRegion = Region;
        }
        return config;
    }

    internal IAmazonS3 CreateClient()
    {
        if (!IsValid(this)) throw new InvalidOperationException("Configure a valid S3 bucket, region, endpoint and credential pair.");
        var config = CreateClientConfig();
        if (string.IsNullOrWhiteSpace(AccessKeyId)) return new AmazonS3Client(config); // AWS default credential chain / IAM role.
        AWSCredentials credentials = string.IsNullOrWhiteSpace(SessionToken)
            ? new BasicAWSCredentials(AccessKeyId, SecretAccessKey)
            : new SessionAWSCredentials(AccessKeyId, SecretAccessKey, SessionToken);
        return new AmazonS3Client(credentials, config);
    }

    internal string AreaPrefix(string area) => (Prefix.Length == 0 ? "" : Prefix.TrimEnd('/') + "/") + area + "/";
}
