namespace Saas.Subscription.Sample.Application.Identity;

public enum PasswordVerificationResult
{
    Failed,
    Succeeded,

    /// <summary>The password is right but the stored hash is weaker than the current algorithm or work factor; store a new hash.</summary>
    SucceededRehashNeeded,
}
