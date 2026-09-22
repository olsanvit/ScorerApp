using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ScorerApp.Domain.Services;
using ScorerApp.Domain.Services.Clubs;

namespace ScorerApp.Tests;

/// <summary>
/// E-maily modulu Kluby se skládají v jazyce příjemce přes CultureScope — tady se ověřuje,
/// že scope opravdu přepne jazyk textu, po skončení vrátí původní a že uživatelský text se escapuje.
/// </summary>
public class ClubMailsTests
{
    private static readonly IStringLocalizer<SharedResource> Localizer =
        new StringLocalizer<SharedResource>(new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions { ResourcesPath = "Resources" }),
            NullLoggerFactory.Instance));

    [Theory]
    [InlineData("en", "Invitation to the club", "Accept invitation")]
    [InlineData("cs", "Pozvánka do oddílu", "Přijmout pozvánku")]
    [InlineData(null, "Pozvánka do oddílu", "Přijmout pozvánku")]      // bez preference = čeština
    [InlineData("de", "Pozvánka do oddílu", "Přijmout pozvánku")]      // nepodporovaný jazyk = čeština
    [InlineData("en-GB", "Invitation to the club", "Accept invitation")]
    public void Invitation_UsesRecipientLanguage(string? culture, string subjectStart, string acceptText)
    {
        (string Subject, string Html) mail;
        using (CultureScope.For(culture))
            mail = ClubMails.Invitation(Localizer, "Klub", "Org", "https://example.test/accept-invite?token=x");

        Assert.StartsWith(subjectStart, mail.Subject);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(acceptText), mail.Html);
        Assert.Contains("href=\"https://example.test/accept-invite?token=x\"", mail.Html);
    }

    [Fact]
    public void Scope_RestoresOriginalCulture()
    {
        var original = CultureInfo.CurrentUICulture;
        using (CultureScope.For("en"))
            Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        Assert.Equal(original, CultureInfo.CurrentUICulture);
    }

    [Fact]
    public void UserText_IsHtmlEscaped()
    {
        string html;
        using (CultureScope.For("en"))
            html = ClubMails.ChatMessage(Localizer, "<b>Eva</b>", "<script>x</script>", "a\n<img src=x>");

        Assert.Contains("wrote in thread", html);
        Assert.DoesNotContain("<b>Eva</b>", html);
        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img", html);
        Assert.Contains("a<br>&lt;img", html);
    }
}
