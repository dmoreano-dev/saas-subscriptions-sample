using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Saas.Subscription.Sample.Application.Configuration;
using Saas.Subscription.Sample.Infrastructure.Identity;
using Saas.Subscription.Sample.IntegrationTests.Configuration;

namespace Saas.Subscription.Sample.IntegrationTests.Identity;

/// <summary>One way a token can differ from a token the API would issue.</summary>
public enum Forgery
{
    /// <summary>No difference: the control that proves the forge itself builds acceptable tokens.</summary>
    None,
    ExpiredInsideClockSkew,
    NotYetValidInsideClockSkew,

    AlgNoneWithoutSignature,
    SignatureRemoved,
    SignatureTampered,
    PayloadTampered,
    SignedWithAnotherKey,
    UnknownKeyId,
    MissingKeyId,
    HmacWithThePublicKeyAsSecret,
    Es384FromAnotherCurve,
    WrongIssuer,
    MissingIssuer,
    WrongAudience,
    MissingAudience,
    Expired,
    MissingExpiry,
    NotYetValid,
    NotBeforeAfterExpiry,
    MissingSubject,
    SubjectIsNotAUserId,
    MissingTokenId,
}

/// <summary>
/// Builds compact JWTs by hand (JSON, base64url, raw ECDSA/HMAC), independently of the token library under test,
/// so each rejected token differs from an accepted one in exactly one way.
/// </summary>
internal static class TokenForge
{
    private const string Es256 = "ES256";
    private const string OtherApi = "some-other-api";
    private const string OtherIssuer = "some-other-issuer";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    public static string Create(Forgery forgery, AuthenticationOptions options, DateTimeOffset now)
    {
        var skew = AccessTokenValidation.ClockSkew;
        var header = new Dictionary<string, object> { ["alg"] = Es256, ["typ"] = "JWT", ["kid"] = TestSigningKey.KeyId };
        var payload = new Dictionary<string, object>
        {
            ["iss"] = options.Issuer!,
            ["aud"] = options.Audience!,
            ["sub"] = Guid.CreateVersion7().ToString(),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = (now + Lifetime).ToUnixTimeSeconds(),
        };

        switch (forgery)
        {
            case Forgery.ExpiredInsideClockSkew:
                SetLifetime(payload, now - Lifetime - skew, now - skew);
                break;
            case Forgery.Expired:
                SetLifetime(payload, now - Lifetime - skew - OneSecond, now - skew - OneSecond);
                break;
            case Forgery.NotYetValidInsideClockSkew:
                SetLifetime(payload, now + skew, now + skew + Lifetime);
                break;
            case Forgery.NotYetValid:
                SetLifetime(payload, now + skew + OneSecond, now + skew + OneSecond + Lifetime);
                break;
            case Forgery.NotBeforeAfterExpiry:
                payload["nbf"] = (now + Lifetime + OneSecond).ToUnixTimeSeconds();
                break;
            case Forgery.MissingExpiry:
                payload.Remove("exp");
                break;
            case Forgery.WrongIssuer:
                payload["iss"] = OtherIssuer;
                break;
            case Forgery.MissingIssuer:
                payload.Remove("iss");
                break;
            case Forgery.WrongAudience:
                payload["aud"] = OtherApi;
                break;
            case Forgery.MissingAudience:
                payload.Remove("aud");
                break;
            case Forgery.MissingSubject:
                payload.Remove("sub");
                break;
            case Forgery.SubjectIsNotAUserId:
                payload["sub"] = "ana@example.com";
                break;
            case Forgery.MissingTokenId:
                payload.Remove("jti");
                break;
            case Forgery.UnknownKeyId:
                header["kid"] = "another-key-id";
                break;
            case Forgery.MissingKeyId:
                header.Remove("kid");
                break;
            case Forgery.AlgNoneWithoutSignature:
                header["alg"] = "none";
                break;
            case Forgery.HmacWithThePublicKeyAsSecret:
                header["alg"] = "HS256";
                break;
            case Forgery.Es384FromAnotherCurve:
                header["alg"] = "ES384";
                break;
        }

        var signingInput = $"{Encode(header)}.{Encode(payload)}";
        var signature = forgery switch
        {
            Forgery.AlgNoneWithoutSignature or Forgery.SignatureRemoved => [],
            Forgery.SignedWithAnotherKey => SignWithNewKey(signingInput, ECCurve.NamedCurves.nistP256, HashAlgorithmName.SHA256),
            Forgery.Es384FromAnotherCurve => SignWithNewKey(signingInput, ECCurve.NamedCurves.nistP384, HashAlgorithmName.SHA384),
            Forgery.HmacWithThePublicKeyAsSecret => HMACSHA256.HashData(TestSigningKey.Key.ExportSubjectPublicKeyInfo(), Ascii(signingInput)),
            _ => TestSigningKey.Key.SignData(Ascii(signingInput), HashAlgorithmName.SHA256),
        };

        if (forgery == Forgery.SignatureTampered)
        {
            signature[0] ^= 0x01;
        }

        if (forgery == Forgery.PayloadTampered)
        {
            // The signature still covers the original payload; only the subject was swapped afterwards.
            payload["sub"] = Guid.CreateVersion7().ToString();
            signingInput = $"{Encode(header)}.{Encode(payload)}";
        }

        return $"{signingInput}.{Base64Url.EncodeToString(signature)}";
    }

    /// <summary>
    /// A token the API accepts (right key, issuer, audience and lifetime) for a real user, carrying extra claims
    /// the API never issues. It stands for a stale or self-serving copy of a role, plan or account label.
    /// </summary>
    public static string CreateWithExtraClaims(AuthenticationOptions options, DateTimeOffset now, Guid subject, IReadOnlyDictionary<string, object> extraClaims)
    {
        var header = new Dictionary<string, object> { ["alg"] = Es256, ["typ"] = "JWT", ["kid"] = TestSigningKey.KeyId };
        var payload = new Dictionary<string, object>(extraClaims)
        {
            ["iss"] = options.Issuer!,
            ["aud"] = options.Audience!,
            ["sub"] = subject.ToString(),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["exp"] = (now + Lifetime).ToUnixTimeSeconds(),
        };

        var signingInput = $"{Encode(header)}.{Encode(payload)}";
        return $"{signingInput}.{Base64Url.EncodeToString(TestSigningKey.Key.SignData(Ascii(signingInput), HashAlgorithmName.SHA256))}";
    }

    private static void SetLifetime(Dictionary<string, object> payload, DateTimeOffset notBefore, DateTimeOffset expires)
    {
        payload["iat"] = notBefore.ToUnixTimeSeconds();
        payload["nbf"] = notBefore.ToUnixTimeSeconds();
        payload["exp"] = expires.ToUnixTimeSeconds();
    }

    private static byte[] SignWithNewKey(string signingInput, ECCurve curve, HashAlgorithmName hash)
    {
        using var key = ECDsa.Create(curve);
        return key.SignData(Ascii(signingInput), hash);
    }

    private static string Encode(Dictionary<string, object> json) => Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(json));

    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
}
