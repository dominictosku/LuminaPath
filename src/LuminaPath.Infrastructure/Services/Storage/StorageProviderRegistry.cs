using LuminaPath.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaPath.Infrastructure.Services.Storage;

/// <summary>Resolves independently configured providers, including inactive migration endpoints.</summary>
public sealed class StorageProviderRegistry(IEnumerable<StorageProviderRegistration> registrations, IServiceProvider services)
{
    private readonly Dictionary<string, StorageProviderRegistration> _providers = registrations
        .ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Names => _providers.Keys;

    public IStorageService Get(string name)
    {
        if (!_providers.TryGetValue(name, out var provider))
            throw new InvalidOperationException($"Unknown storage provider '{name}'. Registered providers: {string.Join(", ", Names)}.");
        return services.GetRequiredKeyedService<IStorageService>(provider.Name);
    }
}

public sealed record StorageProviderRegistration(string Name);

public static class StorageProviderRegistrationExtensions
{
    /// <summary>Register once to enable both normal storage and migration. Providers must be thread safe.</summary>
    public static IServiceCollection AddStorageProvider(this IServiceCollection services, string name,
        Func<IServiceProvider, IStorageService> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (services.Any(service => service.ImplementationInstance is StorageProviderRegistration existing
            && existing.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Storage provider '{name}' is already registered.");
        services.AddSingleton(new StorageProviderRegistration(name));
        services.AddKeyedSingleton<IStorageService>(name, (sp, _) => factory(sp));
        return services;
    }
}
