using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.IntegrationTests.Persistence;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

[Collection(PostgresTestGroup.Name)]
public class LoginServiceTests(PostgresFixture postgres)
{
    private const string UnknownEmail = "nobody@example.com";
    private const string NoPasswordEmail = "sso@example.com";
    private const string DisabledEmail = "off@example.com";
    private const int WeakIterations = PasswordHashingOptions.MinIterations;

    private static readonly LoginThrottlePolicy SmallPolicy = new(MaxAttempts: 3, Window: TimeSpan.FromMinutes(15), MaxTrackedKeys: 100);

    private static async Task SeedUserAsync(IdentityHarness harness, string email, bool withPassword, string status = "Active")
    {
        await using var connection = await Sql.OpenAsync(harness.ConnectionString);
        var id = Sql.NewId();
        await Sql.InsertUserAsync(connection, id, email);
        await Sql.ExecuteAsync(connection, "UPDATE users SET status = @status WHERE id = @id", ("status", status), ("id", id));
        if (withPassword)
        {
            var hash = harness.Hasher.Hash(IdentityHarness.Password);
            await Sql.InsertPasswordCredentialAsync(connection, id, hash.Algorithm, hash.EncodedHash);
        }
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsTheAuthenticatedIdentity()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        var registered = await harness.RegisterAsync();

        // Act
        var result = await harness.LoginAsync();

        // Assert
        Assert.Equal(LoginOutcome.Succeeded, result.Outcome);
        Assert.Equal(new AuthenticatedIdentity(registered.User.UserId, IdentityHarness.Email, registered.User.CreatedAt), result.Identity);
    }

    [Fact]
    public async Task LoginAsync_EmailInDifferentCase_Succeeds()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        await harness.RegisterAsync();

        // Act
        var result = await harness.LoginAsync(" ANA@Example.com ");

        // Assert
        Assert.Equal(LoginOutcome.Succeeded, result.Outcome);
    }

    [Theory]
    [InlineData(IdentityHarness.Email, IdentityHarness.OtherPassword)]
    [InlineData(UnknownEmail, IdentityHarness.Password)]
    [InlineData(NoPasswordEmail, IdentityHarness.Password)]
    [InlineData(DisabledEmail, IdentityHarness.Password)]
    public async Task LoginAsync_AnyKindOfFailure_ReturnsTheSameGenericResult(string email, string password)
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        await harness.RegisterAsync();
        await SeedUserAsync(harness, NoPasswordEmail, withPassword: false);
        await SeedUserAsync(harness, DisabledEmail, withPassword: true, status: nameof(UserStatus.Disabled));

        // Act
        var result = await harness.LoginAsync(email, password);

        // Assert
        Assert.Equal(LoginResult.InvalidCredentials, result);
    }

    [Theory]
    [InlineData(UnknownEmail)]
    [InlineData(NoPasswordEmail)]
    [InlineData(DisabledEmail)]
    [InlineData(IdentityHarness.Email)]
    public async Task LoginAsync_AnyLookupOutcome_VerifiesExactlyOnePassword(string email)
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        await harness.RegisterAsync();
        await SeedUserAsync(harness, NoPasswordEmail, withPassword: false);
        await SeedUserAsync(harness, DisabledEmail, withPassword: true, status: nameof(UserStatus.Disabled));
        var verificationsBefore = harness.Hasher.VerifyCalls;

        // Act
        await harness.LoginAsync(email, IdentityHarness.OtherPassword);

        // Assert
        Assert.Equal(1, harness.Hasher.VerifyCalls - verificationsBefore);
    }

    [Theory]
    [InlineData("", IdentityHarness.Password)]
    [InlineData(IdentityHarness.Email, "")]
    public async Task LoginAsync_BlankInput_IsInvalidCredentialsWithoutAHash(string email, string password)
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        await harness.RegisterAsync();
        var verificationsBefore = harness.Hasher.VerifyCalls;

        // Act
        var result = await harness.LoginAsync(email, password);

        // Assert
        Assert.Equal(LoginResult.InvalidCredentials, result);
        Assert.Equal(verificationsBefore, harness.Hasher.VerifyCalls);
    }

    [Fact]
    public async Task LoginAsync_HashWithWeakerWorkFactor_SucceedsAndStoresAnUpgradedHash()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        var registered = await harness.RegisterAsync();
        var weak = IdentityHarness.CreateHasher(WeakIterations).Hash(IdentityHarness.Password);
        await using (var connection = await Sql.OpenAsync(harness.ConnectionString))
        {
            await Sql.ExecuteAsync(
                connection,
                "UPDATE password_credentials SET encoded_hash = @hash WHERE user_id = @id",
                ("hash", weak.EncodedHash), ("id", registered.User.UserId));
        }

        var strongerHasher = IdentityHarness.CreateHasher(WeakIterations + 1);
        var stronger = harness.WithIterations(WeakIterations + 1);

        // Act
        var result = await stronger.LoginAsync();

        // Assert
        Assert.Equal(LoginOutcome.Succeeded, result.Outcome);
        await using var verify = await Sql.OpenAsync(harness.ConnectionString);
        var stored = await Sql.ScalarAsync<string>(verify, "SELECT encoded_hash FROM password_credentials WHERE user_id = @id", ("id", registered.User.UserId));
        Assert.NotEqual(weak.EncodedHash, stored);
        Assert.Equal(
            PasswordVerificationResult.Succeeded,
            strongerHasher.Verify(IdentityHarness.Password, new StoredPasswordHash(weak.Algorithm, stored)));
    }

    [Fact]
    public async Task LoginAsync_CurrentWorkFactor_LeavesTheStoredHashUntouched()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres);
        var registered = await harness.RegisterAsync();
        await using var connection = await Sql.OpenAsync(harness.ConnectionString);
        var before = await Sql.ScalarAsync<string>(connection, "SELECT encoded_hash FROM password_credentials WHERE user_id = @id", ("id", registered.User.UserId));

        // Act
        await harness.LoginAsync();

        // Assert
        var after = await Sql.ScalarAsync<string>(connection, "SELECT encoded_hash FROM password_credentials WHERE user_id = @id", ("id", registered.User.UserId));
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task LoginAsync_TooManyFailures_ThrottlesEvenTheCorrectPasswordUntilTheWindowEnds()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres, SmallPolicy);
        await harness.RegisterAsync();
        for (var i = 0; i < SmallPolicy.MaxAttempts; i++)
        {
            await harness.LoginAsync(IdentityHarness.Email, IdentityHarness.OtherPassword);
        }

        // Act
        var blocked = await harness.LoginAsync();
        harness.Time.Advance(SmallPolicy.Window);
        var afterWindow = await harness.LoginAsync();

        // Assert
        Assert.Equal(LoginOutcome.Throttled, blocked.Outcome);
        Assert.Equal(SmallPolicy.Window, blocked.RetryAfter);
        Assert.Equal(LoginOutcome.Succeeded, afterWindow.Outcome);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmailFailingRepeatedly_IsThrottledLikeAKnownOne()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres, SmallPolicy);
        for (var i = 0; i < SmallPolicy.MaxAttempts; i++)
        {
            await harness.LoginAsync(UnknownEmail, IdentityHarness.Password);
        }

        // Act
        var result = await harness.LoginAsync(UnknownEmail, IdentityHarness.Password);

        // Assert
        Assert.Equal(LoginOutcome.Throttled, result.Outcome);
    }

    [Fact]
    public async Task LoginAsync_SuccessBetweenFailures_ResetsTheAttemptCount()
    {
        // Arrange
        var harness = await IdentityHarness.CreateAsync(postgres, SmallPolicy);
        await harness.RegisterAsync();
        for (var i = 0; i < SmallPolicy.MaxAttempts - 1; i++)
        {
            await harness.LoginAsync(IdentityHarness.Email, IdentityHarness.OtherPassword);
        }

        await harness.LoginAsync();
        for (var i = 0; i < SmallPolicy.MaxAttempts - 1; i++)
        {
            await harness.LoginAsync(IdentityHarness.Email, IdentityHarness.OtherPassword);
        }

        // Act
        var result = await harness.LoginAsync();

        // Assert
        Assert.Equal(LoginOutcome.Succeeded, result.Outcome);
    }
}
