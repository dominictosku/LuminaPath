using System.Diagnostics;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LuminaPath.Infrastructure.Services.Application;

public sealed class DatabaseBackupService
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseBackupOptions _options;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly Func<DateTimeOffset> _now;

    public DatabaseBackupService(
        IConfiguration configuration,
        IOptions<DatabaseBackupOptions> options,
        ILogger<DatabaseBackupService> logger)
        : this(configuration, options, logger, () => DateTimeOffset.UtcNow)
    {
    }

    internal DatabaseBackupService(
        IConfiguration configuration,
        IOptions<DatabaseBackupOptions> options,
        ILogger<DatabaseBackupService> logger,
        Func<DateTimeOffset> now)
    {
        _configuration = configuration;
        _options = options.Value;
        _logger = logger;
        _now = now;
    }

    public Task<IReadOnlyList<DatabaseBackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        var directory = GetBackupDirectory();
        if (!Directory.Exists(directory))
        {
            return Task.FromResult<IReadOnlyList<DatabaseBackupInfo>>(Array.Empty<DatabaseBackupInfo>());
        }

        var backups = Directory
            .EnumerateFiles(directory, "*.dump", SearchOption.TopDirectoryOnly)
            .Select(CreateBackupInfo)
            .OrderByDescending(backup => backup.LastModifiedAt)
            .Take(Math.Max(_options.MaxListedBackups, 1))
            .ToList();

        return Task.FromResult<IReadOnlyList<DatabaseBackupInfo>>(backups);
    }

    public Task<int> DeleteOldBackupsAsync(int keepCount, CancellationToken cancellationToken = default)
    {
        var directory = GetBackupDirectory();
        if (!Directory.Exists(directory))
        {
            return Task.FromResult(0);
        }

        var backupsToDelete = Directory
            .EnumerateFiles(directory, "*.dump", SearchOption.TopDirectoryOnly)
            .Select(CreateBackupInfo)
            .OrderByDescending(backup => backup.LastModifiedAt)
            .Skip(Math.Max(keepCount, 0))
            .ToList();

        var deleted = 0;
        foreach (var backup in backupsToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                File.Delete(backup.FullPath);
                deleted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete old database backup {BackupFile}.", backup.FileName);
            }
        }

        return Task.FromResult(deleted);
    }

    public async Task<Result<DatabaseBackupInfo, FailedResult>> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = ConfigurationValues.FirstNonEmpty(
            _configuration.GetConnectionString("Default"),
            _configuration["POSTGRESQL_DB"]);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new FailedResult("Missing database connection string.");
        }

        NpgsqlConnectionStringBuilder connection;
        try
        {
            connection = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse PostgreSQL connection string for backup.");
            return new FailedResult("Database connection string could not be parsed.");
        }

        if (string.IsNullOrWhiteSpace(connection.Database))
        {
            return new FailedResult("Database name is missing from the connection string.");
        }

        var directory = GetBackupDirectory();
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, CreateBackupFileName(connection.Database));
        var processInfo = CreatePgDumpStartInfo(connection, filePath);

        try
        {
            using var process = Process.Start(processInfo);
            if (process is null)
            {
                return new FailedResult("Could not start pg_dump.");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                DeletePartialBackup(filePath);
                var detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                _logger.LogWarning("pg_dump failed with exit code {ExitCode}. Details: {Details}", process.ExitCode, detail);
                return new FailedResult(string.IsNullOrWhiteSpace(detail)
                    ? $"pg_dump failed with exit code {process.ExitCode}."
                    : detail.Trim());
            }

            return CreateBackupInfo(filePath);
        }
        catch (OperationCanceledException)
        {
            DeletePartialBackup(filePath);
            throw;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            DeletePartialBackup(filePath);
            _logger.LogWarning(ex, "pg_dump executable was not found.");
            return new FailedResult("pg_dump was not found. Install PostgreSQL client tools or configure DatabaseBackup:PgDumpPath.");
        }
        catch (Exception ex)
        {
            DeletePartialBackup(filePath);
            _logger.LogWarning(ex, "Could not create PostgreSQL backup.");
            return new FailedResult("Could not create database backup.");
        }
    }

    private ProcessStartInfo CreatePgDumpStartInfo(NpgsqlConnectionStringBuilder connection, string filePath)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = _options.PgDumpPath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        processInfo.ArgumentList.Add("--format=custom");
        processInfo.ArgumentList.Add("--file");
        processInfo.ArgumentList.Add(filePath);

        if (!string.IsNullOrWhiteSpace(connection.Host))
        {
            processInfo.ArgumentList.Add("--host");
            processInfo.ArgumentList.Add(connection.Host);
        }

        if (connection.Port > 0)
        {
            processInfo.ArgumentList.Add("--port");
            processInfo.ArgumentList.Add(connection.Port.ToString());
        }

        if (!string.IsNullOrWhiteSpace(connection.Username))
        {
            processInfo.ArgumentList.Add("--username");
            processInfo.ArgumentList.Add(connection.Username);
        }

        processInfo.ArgumentList.Add("--dbname");
        processInfo.ArgumentList.Add(connection.Database!);

        if (!string.IsNullOrWhiteSpace(connection.Password))
        {
            processInfo.Environment["PGPASSWORD"] = connection.Password;
        }

        return processInfo;
    }

    private string GetBackupDirectory()
    {
        return Path.GetFullPath(_options.Directory);
    }

    private string CreateBackupFileName(string databaseName)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.FilePrefix) ? "luminapath" : _options.FilePrefix.Trim();
        var safeDatabaseName = SanitizeFileName(databaseName);
        return $"{SanitizeFileName(prefix)}-{safeDatabaseName}-{_now():yyyyMMdd-HHmmss}.dump";
    }

    private static string SanitizeFileName(string value)
    {
        var sanitized = string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character)).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "database" : sanitized;
    }

    private static DatabaseBackupInfo CreateBackupInfo(string filePath)
    {
        var file = new FileInfo(filePath);
        return new DatabaseBackupInfo(
            file.Name,
            file.FullName,
            file.CreationTimeUtc,
            file.LastWriteTimeUtc,
            file.Exists ? file.Length : 0);
    }

    private static void DeletePartialBackup(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}

public sealed record DatabaseBackupInfo(
    string FileName,
    string FullPath,
    DateTime CreatedAt,
    DateTime LastModifiedAt,
    long SizeBytes);
