using ScorerApp.Domain.Services;

namespace ScorerApp.Tests;

public class CanonicalHostRedirectTests
{
    private const string Base = "https://scorer.vo2info.cz";

    [Fact]
    public void OtherDomain_RedirectsToCanonicalWithPathAndQuery() =>
        Assert.Equal("https://scorer.vo2info.cz/login?ReturnUrl=%2F",
            CanonicalHostRedirect.Target(Base, "scorerapp.vo2info.cz", "", "/login", "?ReturnUrl=%2F"));

    [Theory]
    [InlineData("scorer.vo2info.cz")]
    [InlineData("SCORER.vo2info.cz:443")]
    [InlineData("127.0.0.1:5021")]
    [InlineData("localhost:5021")]
    [InlineData("[::1]:5021")]
    [InlineData("scorerapp")]
    public void CanonicalHostIpAndLanNames_AreNotRedirected(string host) =>
        Assert.Null(CanonicalHostRedirect.Target(Base, host, "", "/health", ""));

    [Fact]
    public void MissingBaseUrl_DisablesRedirect() =>
        Assert.Null(CanonicalHostRedirect.Target(null, "scorerapp.vo2info.cz", "", "/", ""));

    [Fact]
    public void BaseUrlWithPathBase_KeepsRequestPathOnly() =>
        Assert.Equal("https://vo2info.cz/scorer/x",
            CanonicalHostRedirect.Target("https://vo2info.cz/scorer", "www.vo2info.cz", "", "/scorer/x", ""));
}
