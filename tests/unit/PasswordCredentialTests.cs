using Saas.Subscription.Sample.Domain.Identity;

namespace Saas.Subscription.Sample.UnitTests;

public class PasswordCredentialTests
{
    private static readonly DateTimeOffset CreatedAt = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset LaterAt = DateTimeOffset.UnixEpoch.AddDays(1);
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly StoredPasswordHash FirstHash = new("argon2id", "first");
    private static readonly StoredPasswordHash SecondHash = new("argon2id", "second");

    [Fact]
    public void Create_StoredHash_KeepsEveryPart()
    {
        // Act
        var credential = PasswordCredential.Create(UserId, FirstHash, CreatedAt);

        // Assert
        Assert.Equal(UserId, credential.UserId);
        Assert.Equal(FirstHash, credential.ToStoredHash());
    }

    [Fact]
    public void Rehash_NewHash_ReplacesTheHashAndKeepsCreationTime()
    {
        // Arrange
        var credential = PasswordCredential.Create(UserId, FirstHash, CreatedAt);

        // Act
        credential.Rehash(SecondHash, LaterAt);

        // Assert
        Assert.Equal(SecondHash, credential.ToStoredHash());
        Assert.Equal(CreatedAt, credential.CreatedAt);
        Assert.Equal(LaterAt, credential.UpdatedAt);
    }
}
