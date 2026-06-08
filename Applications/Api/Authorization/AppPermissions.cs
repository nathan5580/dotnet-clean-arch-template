namespace Api.Authorization;

public static class AppPermissions
{
    // Auth
    public const string UsersRead = "users.read";
    public const string UsersWrite = "users.write";

    // Add your permission constants here
}

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string User = "User";
}
