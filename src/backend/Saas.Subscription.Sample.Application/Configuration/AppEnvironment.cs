namespace Saas.Subscription.Sample.Application.Configuration;

/// <summary>
/// Where the process runs, derived from <c>ASPNETCORE_ENVIRONMENT</c>. Only <c>Development</c> is local;
/// every other name is hosted, so a forgotten or mistyped variable gets the strict rules instead of the lenient ones.
/// </summary>
/// <param name="Name">The ASP.NET Core environment name.</param>
/// <param name="IsHosted">True for any environment other than Development.</param>
public sealed record AppEnvironment(string Name, bool IsHosted);
