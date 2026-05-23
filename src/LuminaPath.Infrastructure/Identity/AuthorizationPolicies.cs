namespace LuminaPath.Infrastructure.Identity;

/// <summary>
/// Authorization policy names registered in <see cref="IdentityServiceCollectionExtensions"/>.
/// Keep policy strings here so producers (AddPolicy) and consumers
/// ([Authorize(Policy = ...)] / RequireAuthorization(...)) stay in sync.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Grants users in the Administrator or Editor roles the ability to
    /// mutate the shared catalog (Games / Animes / Series / Movies).
    /// </summary>
    public const string CatalogEditors = "CatalogEditors";
}
