namespace LuminaPath.Tests.Infrastructure;

public class TwoFactorSecuritySourceTests
{
    [Fact]
    public void EnableAuthenticator_RendersQrCodeLocally()
    {
        var source = ReadServerSource("Features", "Auth", "Account", "Pages", "Manage", "EnableAuthenticator.razor");

        Assert.Contains("SvgQRCode", source);
        Assert.Contains("AuthenticatorIssuer = \"LuminaPath\"", source);
        Assert.Contains("authenticator-qr", source);
        Assert.DoesNotContain("Linkid=852423", source);
    }

    [Fact]
    public void PasswordLogin_EnforcesLockoutAndDoesNotTrustRememberedTwoFactorCookie()
    {
        var source = ReadServerSource("Features", "Auth", "Account", "Pages", "Login.razor");

        Assert.Contains("ForgetTwoFactorClientAsync", source);
        Assert.Contains("lockoutOnFailure: true", source);
    }

    [Fact]
    public void TwoFactorLogin_DoesNotAllowRememberingCurrentMachine()
    {
        var source = ReadServerSource("Features", "Auth", "Account", "Pages", "LoginWith2fa.razor");

        Assert.Contains("rememberClient: false", source);
        Assert.Contains("rememberMachine = false", source);
        Assert.DoesNotContain("Remember this machine", source);
        Assert.DoesNotContain("RememberMachine", source);
    }

    [Fact]
    public void IdentityApiLogin_ForgetsRememberedTwoFactorCookieBeforePasswordSignIn()
    {
        var source = ReadInfrastructureSource("Services", "Auditing", "IdentityEndpointAuditMiddleware.cs");

        Assert.Contains("request.Action == AuditActions.Login", source);
        Assert.Contains("ForgetTwoFactorClientAsync", source);
    }

    private static string ReadServerSource(params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "LuminaPath", Path.Combine(pathParts)));
    }

    private static string ReadInfrastructureSource(params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "LuminaPath.Infrastructure", Path.Combine(pathParts)));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "LuminaPath", "LuminaPath.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the LuminaPath repository root.");
    }
}
