using Saas.Subscription.Sample.Application.Configuration;

namespace Saas.Subscription.Sample.UnitTests.Configuration;

internal static class OptionsTestData
{
    public static readonly AppEnvironment Local = new("Development", IsHosted: false);
    public static readonly AppEnvironment Hosted = new("Production", IsHosted: true);

    public const string SecureConnectionString = "Host=h;Database=d;Username=u;Password=p;SSL Mode=VerifyFull";
    public const string TestSecretKey = "sk_test_example";
    public const string LiveSecretKey = "sk_live_example";
    public const string WebhookSecret = "whsec_example";
    public const string HttpsOrigin = "https://app.example.test";
}
