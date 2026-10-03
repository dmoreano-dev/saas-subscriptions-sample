using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Application.Identity;
using Saas.Subscription.Sample.Infrastructure.Identity;

namespace Saas.Subscription.Sample.UnitTests.Identity;

public sealed class JwtAccessTokenIssuerTests : IDisposable
{
    private const string Issuer = "test-issuer";
    private const string Audience = "test-audience";
    private const string Es256 = "ES256";
    private const string JwtType = "JWT";

    private static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private static readonly Guid UserId = Guid.Parse("0197c0de-0000-7000-8000-000000000001");
    private static readonly string[] ExpectedClaims = ["aud", "exp", "iat", "iss", "jti", "nbf", "sub"];

    private readonly FakeTimeProvider _time = new(Now);
    private readonly AccessTokenSigningKeys _keys;
    private readonly IOptions<AuthenticationOptions> _options = Options.Create(new AuthenticationOptions
    {
        Issuer = Issuer,
        Audience = Audience,
        AccessTokenLifetime = Lifetime,
        SigningKey = TestKeys.P256Pkcs8Pem,
        SigningKeyId = TestKeys.KeyId,
    });

    public JwtAccessTokenIssuerTests() => _keys = new AccessTokenSigningKeys(_options);

    public void Dispose() => _keys.Dispose();

    private JwtAccessTokenIssuer CreateIssuer() => new(_options, _keys, _time);

    private static bool IsSignedBy(JsonWebToken token, string privateKeyPem)
    {
        using var key = ECDsa.Create();
        key.ImportFromPem(privateKeyPem);
        return key.VerifyData(
            Encoding.ASCII.GetBytes($"{token.EncodedHeader}.{token.EncodedPayload}"),
            Base64Url.DecodeFromChars(token.EncodedSignature),
            HashAlgorithmName.SHA256);
    }

    [Fact]
    public void Issue_UserId_WritesIdentityClaimsAndNothingElse()
    {
        // Arrange
        var issuer = CreateIssuer();

        // Act
        var token = new JsonWebToken(issuer.Issue(UserId).Value);

        // Assert
        Assert.Equal(ExpectedClaims, token.Claims.Select(claim => claim.Type).Order(StringComparer.Ordinal));
        Assert.Equal(UserId.ToString(), token.Subject);
        Assert.Equal(Issuer, token.Issuer);
        Assert.Equal(Audience, Assert.Single(token.Audiences));
        Assert.False(string.IsNullOrWhiteSpace(token.Id));
    }

    [Fact]
    public void Issue_UserId_SetsEveryTimeClaimFromTheInjectedClock()
    {
        // Arrange
        var issuer = CreateIssuer();

        // Act
        var issued = issuer.Issue(UserId);
        var token = new JsonWebToken(issued.Value);

        // Assert
        Assert.Equal(Now.UtcDateTime, token.IssuedAt);
        Assert.Equal(Now.UtcDateTime, token.ValidFrom);
        Assert.Equal((Now + Lifetime).UtcDateTime, token.ValidTo);
        Assert.Equal(Now + Lifetime, issued.ExpiresAt);
    }

    [Fact]
    public void Issue_ClockBetweenSeconds_ReturnsTheSameExpiryAsTheExpClaim()
    {
        // Arrange
        _time.Advance(TimeSpan.FromMilliseconds(750));
        var issuer = CreateIssuer();

        // Act
        var issued = issuer.Issue(UserId);
        var token = new JsonWebToken(issued.Value);

        // Assert
        Assert.Equal(token.ValidTo, issued.ExpiresAt.UtcDateTime);
        Assert.Equal(Lifetime, token.ValidTo - token.IssuedAt);
    }

    [Fact]
    public void Issue_UserId_SignsWithEs256AndTheConfiguredKeyId()
    {
        // Arrange
        var issuer = CreateIssuer();

        // Act
        var token = new JsonWebToken(issuer.Issue(UserId).Value);

        // Assert
        Assert.Equal(Es256, token.Alg);
        Assert.Equal(TestKeys.KeyId, token.Kid);
        Assert.Equal(JwtType, token.Typ);
        Assert.True(IsSignedBy(token, TestKeys.P256Pkcs8Pem));
    }

    [Fact]
    public void Issue_SameUserTwice_UsesADifferentTokenIdEachTime()
    {
        // Arrange
        var issuer = CreateIssuer();
        var first = new JsonWebToken(issuer.Issue(UserId).Value);

        // Act
        var second = new JsonWebToken(issuer.Issue(UserId).Value);

        // Assert
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Issue_UserId_ProducesATokenTheValidationParametersAccept()
    {
        // Arrange
        var issuer = CreateIssuer();
        var parameters = AccessTokenValidation.CreateParameters(_options.Value, _keys, _time);
        var token = issuer.Issue(UserId).Value;

        // Act
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateParameters_Algorithms_AllowOnlyEs256()
    {
        // Act
        var parameters = AccessTokenValidation.CreateParameters(_options.Value, _keys, _time);

        // Assert
        Assert.Equal([Es256], parameters.ValidAlgorithms);
    }

    [Fact]
    public void Validation_Key_HoldsNoPrivateKey()
    {
        // Arrange
        var validationKey = Assert.IsType<ECDsaSecurityKey>(_keys.Validation);

        // Act
        Action exportPrivateKey = () => validationKey.ECDsa.ExportParameters(includePrivateParameters: true);

        // Assert
        Assert.ThrowsAny<CryptographicException>(exportPrivateKey);
    }
}
