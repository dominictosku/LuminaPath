using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LuminaPath.Infrastructure.Services.Storage;

/// <summary>Installation-wide grant, kept outside uploaded files and database backups.</summary>
public sealed class GoogleDriveConnectionStore
{
    private readonly GoogleDriveOptions _options;
    private readonly string _directory;
    private readonly object _gate = new();
    private IDataProtector? _protector;

    public GoogleDriveConnectionStore(IOptions<GoogleDriveOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _directory = Path.GetFullPath(_options.ConnectionPath, environment.ContentRootPath);
    }

    private IDataProtector Protector
    {
        get
        {
            if (_protector is not null) return _protector;
            Directory.CreateDirectory(_directory);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(_directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            // A dedicated persistent key ring also works for the one-off storage migration process.
            _protector = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(_directory, "keys")),
                    builder => builder.SetApplicationName("LuminaPath.GoogleDrive"))
                .CreateProtector("Connection.v1");
            return _protector;
        }
    }

    public GoogleDriveConnection? Read()
    {
        lock (_gate)
        {
            var path = Path.Combine(_directory, "connection.protected");
            if (!File.Exists(path)) return null;
            var connection = JsonSerializer.Deserialize<GoogleDriveConnection>(Protector.Unprotect(File.ReadAllText(path)))
                ?? throw new InvalidOperationException("Cannot read the Google Drive connection.");
            if (connection.ClientId != _options.ClientId)
                throw new InvalidOperationException("The saved Drive connection belongs to a different OAuth client.");
            return connection;
        }
    }

    public void Save(string refreshToken, string accountId)
    {
        lock (_gate)
        {
            var previous = Read();
            // Changing accounts silently would orphan all existing upload references and cached folder IDs.
            if (previous is not null && previous.AccountId != accountId)
                throw new InvalidOperationException("Reconnect the same Google account used for this installation.");
            var encrypted = Protector.Protect(JsonSerializer.Serialize(
                new GoogleDriveConnection(_options.ClientId, refreshToken, accountId, DateTimeOffset.UtcNow)));
            var temporary = Path.Combine(_directory, Guid.NewGuid() + ".tmp");
            try
            {
                var fileOptions = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
                if (!OperatingSystem.IsWindows()) fileOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                using (var stream = new FileStream(temporary, fileOptions))
                using (var writer = new StreamWriter(stream)) writer.Write(encrypted);
                File.Move(temporary, Path.Combine(_directory, "connection.protected"), overwrite: true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}

public sealed record GoogleDriveConnection(string ClientId, string RefreshToken, string AccountId, DateTimeOffset ConnectedAt);
