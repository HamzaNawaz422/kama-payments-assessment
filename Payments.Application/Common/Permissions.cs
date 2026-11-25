namespace Payments.Application.Common;

public static class Permissions
{
    public const string PaymentsCreate = "Payments.Create";
    public const string PaymentsView = "Payments.View";
    public const string PaymentsProcess = "Payments.Process";
    public const string ProvidersManage = "Providers.Manage";

    public const string ClaimType = "permission";

    public static readonly string[] All = { PaymentsCreate, PaymentsView, PaymentsProcess, ProvidersManage };
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
}