namespace LuminaPath.Infrastructure.Services.Auditing;

public static class AuditCategories
{
    public const string Account = "Account";
    public const string Admin = "Admin";
    public const string System = "System";
}

public static class AuditActions
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string Registration = "Registration";
    public const string PasswordChanged = "PasswordChanged";
    public const string PasswordSet = "PasswordSet";
    public const string PasswordReset = "PasswordReset";
    public const string EmailChangeRequested = "EmailChangeRequested";
    public const string EmailChanged = "EmailChanged";
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserDeleted = "UserDeleted";
    public const string UserActivated = "UserActivated";
    public const string UserLocked = "UserLocked";
    public const string RoleChanged = "RoleChanged";
    public const string ApplicationSettingChanged = "ApplicationSettingChanged";
    public const string BackgroundJobQueued = "BackgroundJobQueued";
    public const string BackgroundJobStarted = "BackgroundJobStarted";
    public const string BackgroundJobRetried = "BackgroundJobRetried";
    public const string BackgroundJobCanceled = "BackgroundJobCanceled";
    public const string DatabaseBackupCreated = "DatabaseBackupCreated";
    public const string DatabaseBackupDownloaded = "DatabaseBackupDownloaded";
    public const string EntityChanged = "EntityChanged";
}

public static class AuditOutcomes
{
    public const string Success = "Success";
    public const string Failure = "Failure";
}
