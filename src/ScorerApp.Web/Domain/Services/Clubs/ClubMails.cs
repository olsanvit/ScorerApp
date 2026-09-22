using System.Net;
using Microsoft.Extensions.Localization;

namespace ScorerApp.Domain.Services.Clubs;

/// <summary>
/// Texty e-mailů modulu Kluby. Volá se uvnitř <see cref="CultureScope"/> nastaveného na jazyk příjemce —
/// odděleně od odesílání, aby šly otestovat bez SMTP.
/// Všechno, co píšou uživatelé (názvy, jména, zprávy), se escapuje — jinak by šlo do e-mailu vložit HTML.
/// </summary>
public static class ClubMails
{
    public static (string Subject, string Html) Invitation(IStringLocalizer S, string clubName, string organizationName, string link)
    {
        var subject = S["Mail_InviteSubject", clubName].Value;
        var html = $"<p>{S["Mail_InviteBody", $"<strong>{Enc(clubName)}</strong>", Enc(organizationName)]}</p>" +
                   $"<p><a href=\"{Enc(link)}\">{Enc(S["Mail_InviteAccept"])}</a></p>" +
                   $"<p>{Enc(S["Mail_InviteValidity"])}</p>";
        return (subject, html);
    }

    public static string ChatMessage(IStringLocalizer S, string sender, string threadTitle, string body) =>
        $"<p>{S["Mail_ChatWrote", $"<strong>{Enc(sender)}</strong>", $"<em>{Enc(threadTitle)}</em>"]}</p>" +
        $"<blockquote>{Enc(body).Replace("\n", "<br>")}</blockquote>";

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
