using System.Text;
using Geralt;
using Microsoft.Extensions.Options;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Domain.Identity;
using Saas.Subscription.Sample.Infrastructure.Identity;

namespace Saas.Subscription.Sample.UnitTests.Identity;

public class Argon2idPasswordHasherTests
{
    private const string ValidPassword = "right-password";
    private const string OtherPassword = "wrong-password";
    private const string TooShortPassword = "abcd";
    private const string PhcPrefix = "$argon2id$v=19$";
    private const string NotAHash = "not-a-hash";
    private const string ForeignHash = "$2a$12$abcdefghijklmnopqrstuuABCDEFGHIJKLMNOPQRSTUVWXYZ012345";
    private const string UnknownAlgorithm = "bcrypt";

    // The OWASP floor keeps the suite fast; production values come from configuration.
    private const int LowMemoryMebibytes = PasswordHashingOptions.MinMemoryMebibytes;
    private const int LowIterations = PasswordHashingOptions.MinIterations;

    private static Argon2idPasswordHasher CreateHasher(int memoryMebibytes = LowMemoryMebibytes, int iterations = LowIterations) =>
        new(Options.Create(new PasswordHashingOptions { MemoryMebibytes = memoryMebibytes, Iterations = iterations }));

    private static StoredPasswordHash WithEncodedHash(StoredPasswordHash stored, string encodedHash) =>
        stored with { EncodedHash = encodedHash };

    /// <summary>A hash of a password the current policy would refuse, as one made before a later policy change.</summary>
    private static StoredPasswordHash HashBypassingPolicy(string password, StoredPasswordHash template)
    {
        var buffer = new char[Argon2id.HashSize];
        Argon2id.ComputeHash(buffer, Encoding.UTF8.GetBytes(password), LowIterations, LowMemoryMebibytes * 1024 * 1024);
        return WithEncodedHash(template, new string(buffer.AsSpan().TrimEnd('\0')));
    }

    // ---- Hash ----

    [Fact]
    public void Hash_ValidPassword_ProducesPhcEncodingWithoutThePassword()
    {
        // Arrange
        var hasher = CreateHasher();

        // Act
        var stored = hasher.Hash(ValidPassword);

        // Assert
        Assert.StartsWith(PhcPrefix, stored.EncodedHash, StringComparison.Ordinal);
        Assert.DoesNotContain(ValidPassword, stored.EncodedHash);
        Assert.DoesNotContain('\0', stored.EncodedHash);
    }

    [Fact]
    public void Hash_ValidPassword_RecordsTheAlgorithm()
    {
        // Arrange
        var hasher = CreateHasher();

        // Act
        var stored = hasher.Hash(ValidPassword);

        // Assert
        Assert.Equal(Argon2idPasswordHasher.Algorithm, stored.Algorithm);
    }

    [Fact]
    public void Hash_ValidPassword_EmbedsTheWorkFactorInTheEncodedHash()
    {
        // Arrange
        var hasher = CreateHasher();
        var expected = $"m={LowMemoryMebibytes * 1024},t={LowIterations},p=1";

        // Act
        var stored = hasher.Hash(ValidPassword);

        // Assert
        Assert.Contains(expected, stored.EncodedHash);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentEncodedHashes()
    {
        // Arrange
        var hasher = CreateHasher();

        // Act
        var first = hasher.Hash(ValidPassword);
        var second = hasher.Hash(ValidPassword);

        // Assert
        Assert.NotEqual(first.EncodedHash, second.EncodedHash);
    }

    [Fact]
    public void Hash_PasswordBreakingThePolicy_ThrowsWithoutEchoingThePassword()
    {
        // Arrange
        var hasher = CreateHasher();

        // Act
        var exception = Assert.Throws<ArgumentException>(() => hasher.Hash(TooShortPassword));

        // Assert
        Assert.DoesNotContain(TooShortPassword, exception.Message);
    }

    [Fact]
    public void StoredPasswordHash_ToString_DoesNotRevealTheEncodedHash()
    {
        // Arrange
        var stored = CreateHasher().Hash(ValidPassword);

        // Act
        var text = stored.ToString();

        // Assert
        Assert.DoesNotContain(stored.EncodedHash, text);
    }

    // ---- Verify ----

    [Fact]
    public void Verify_CorrectPassword_Succeeds()
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = hasher.Hash(ValidPassword);

        // Act
        var result = hasher.Verify(ValidPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Succeeded, result);
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = hasher.Hash(ValidPassword);

        // Act
        var result = hasher.Verify(OtherPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_NonAsciiPassword_Succeeds()
    {
        // Arrange
        const string password = "contraseña-😀-密码";
        var hasher = CreateHasher();
        var stored = hasher.Hash(password);

        // Act
        var result = hasher.Verify(password, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Succeeded, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(NotAHash)]
    [InlineData(ForeignHash)]
    public void Verify_CorruptOrForeignEncoding_FailsWithoutThrowing(string encodedHash)
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = WithEncodedHash(hasher.Hash(ValidPassword), encodedHash);

        // Act
        var result = hasher.Verify(ValidPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_EncodingLongerThanTheBuffer_Fails()
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = WithEncodedHash(hasher.Hash(ValidPassword), new string('a', 1000));

        // Act
        var result = hasher.Verify(ValidPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_UnknownAlgorithm_FailsClosed()
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = hasher.Hash(ValidPassword) with { Algorithm = UnknownAlgorithm };

        // Act
        var result = hasher.Verify(ValidPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_OversizedPassword_Fails()
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = hasher.Hash(ValidPassword);
        var oversized = new string('a', PasswordPolicy.MaxLength + 1);

        // Act
        var result = hasher.Verify(oversized, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_EmptyPassword_Fails()
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = hasher.Hash(ValidPassword);

        // Act
        var result = hasher.Verify(string.Empty, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_PasswordShorterThanTheCurrentMinimum_StillSucceeds()
    {
        // Arrange: a policy raised after the password was set must not lock the person out.
        var hasher = CreateHasher();
        var stored = HashBypassingPolicy(TooShortPassword, hasher.Hash(ValidPassword));

        // Act
        var result = hasher.Verify(TooShortPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Succeeded, result);
    }

    // ---- Rehash ----

    [Fact]
    public void Verify_HashMadeWithTheCurrentParameters_DoesNotAskForRehash()
    {
        // Arrange
        var hasher = CreateHasher();
        var stored = hasher.Hash(ValidPassword);

        // Act
        var result = hasher.Verify(ValidPassword, stored);

        // Assert
        Assert.NotEqual(PasswordVerificationResult.SucceededRehashNeeded, result);
    }

    [Theory]
    [InlineData(LowMemoryMebibytes + 1, LowIterations)]
    [InlineData(LowMemoryMebibytes, LowIterations + 1)]
    public void Verify_CurrentParametersAreStronger_SucceedsAndAsksForRehash(int currentMemory, int currentIterations)
    {
        // Arrange: the encoding carries its own parameters, so raising the options never breaks old hashes.
        var stored = CreateHasher().Hash(ValidPassword);
        var strongerHasher = CreateHasher(currentMemory, currentIterations);

        // Act
        var result = strongerHasher.Verify(ValidPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.SucceededRehashNeeded, result);
    }

    [Fact]
    public void Verify_WrongPasswordWithStrongerCurrentParameters_StillFails()
    {
        // Arrange
        var stored = CreateHasher().Hash(ValidPassword);
        var strongerHasher = CreateHasher(iterations: LowIterations + 1);

        // Act
        var result = strongerHasher.Verify(OtherPassword, stored);

        // Assert
        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Verify_AfterRehashWithTheNewParameters_Succeeds()
    {
        // Arrange
        var strongerHasher = CreateHasher(iterations: LowIterations + 1);
        var rehashed = strongerHasher.Hash(ValidPassword);

        // Act
        var result = strongerHasher.Verify(ValidPassword, rehashed);

        // Assert
        Assert.Equal(PasswordVerificationResult.Succeeded, result);
    }
}
