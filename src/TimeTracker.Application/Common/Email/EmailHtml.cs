using TimeTracker.Application.Common.Interfaces;

namespace TimeTracker.Application.Common.Email;

/// <summary>
/// HTML for emails: one layout with the app's name and colours, and the pieces that go in it.
/// Tables and inline styles only, since that's what mail clients (Outlook above all) draw reliably.
/// Every text is HTML-encoded here; callers pass plain text.
/// </summary>
public static class EmailHtml {
    private const string Font = "font-family:Arial,Helvetica,sans-serif";
    private const string Text = "#27272a";
    private const string Muted = "#71717a";
    private const string Line = "#e4e4e7";
    private const string Page = "#f4f4f5";

    /// <summary>
    /// A whole email: a bar with the app's name, a card with <paramref name="heading"/> and
    /// <paramref name="content"/>, and a small footer under it.
    /// </summary>
    public static string Document(IAppBranding branding, string heading, string content, string? footer = null) {
        var title = Encode(branding.Title);
        var titleHtml = branding.AppUrl is { } url
            ? $"<a href=\"{Encode(url)}\" target=\"_blank\" style=\"color:{branding.ColorOnPrimary};text-decoration:none\">{title}</a>"
            : title;
        var footerHtml = footer is null
            ? ""
            : $"<tr><td style=\"padding:16px 8px 0;{Font};font-size:12px;line-height:18px;color:{Muted};text-align:center\">{Encode(footer)}</td></tr>";

        return "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" +
               "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
               $"<title>{Encode(heading)}</title></head>" +
               $"<body style=\"margin:0;padding:0;background-color:{Page}\">" +
               $"<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"background-color:{Page}\">" +
               "<tr><td align=\"center\" style=\"padding:24px 12px\">" +
               "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"max-width:560px\">" +
               "<tr><td>" +
               $"<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" " +
               $"style=\"background-color:#ffffff;border:1px solid {Line};border-radius:10px;border-collapse:separate\">" +
               $"<tr><td bgcolor=\"{branding.ColorPrimary}\" style=\"background-color:{branding.ColorPrimary};border-radius:9px 9px 0 0;" +
               $"padding:16px 28px;{Font};font-size:17px;font-weight:bold;color:{branding.ColorOnPrimary}\">{titleHtml}</td></tr>" +
               $"<tr><td style=\"padding:28px;{Font};font-size:15px;line-height:22px;color:{Text}\">" +
               $"<h1 style=\"margin:0 0 16px;{Font};font-size:20px;line-height:28px;color:#18181b\">{Encode(heading)}</h1>" +
               content +
               "</td></tr></table>" +
               "</td></tr>" +
               footerHtml +
               "</table></td></tr></table></body></html>";
    }

    public static string Paragraphs(params string?[] paragraphs) =>
        string.Concat(paragraphs.Where(p => p is not null)
            .Select(p => $"<p style=\"margin:0 0 14px\">{Encode(p!)}</p>"));

    /// <summary>A small, grey note, e.g. why someone gets a copy.</summary>
    public static string Note(string? text) =>
        text is null ? "" : $"<p style=\"margin:0 0 14px;font-size:13px;line-height:19px;color:{Muted}\">{Encode(text)}</p>";

    /// <summary>Label and value pairs in a box, e.g. the type, dates and hours of a leave request. Empty values are left out.</summary>
    public static string Details(string accentColor, params (string Label, string? Value)[] rows) {
        var cells = rows.Where(r => !string.IsNullOrWhiteSpace(r.Value)).Select(r =>
            "<tr>" +
            $"<td style=\"padding:6px 16px 6px 0;{Font};font-size:13px;line-height:20px;color:{Muted};white-space:nowrap;vertical-align:top\">{Encode(r.Label)}</td>" +
            $"<td style=\"padding:6px 0;{Font};font-size:15px;line-height:20px;color:{Text};font-weight:bold\">{Encode(r.Value!)}</td>" +
            "</tr>");

        return $"<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" " +
               $"style=\"margin:4px 0 20px;background-color:{Page};border-left:4px solid {accentColor};border-radius:6px\">" +
               $"<tr><td style=\"padding:10px 16px\"><table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">" +
               string.Concat(cells) +
               "</table></td></tr></table>";
    }

    /// <summary>The big code in a password reset email.</summary>
    public static string Code(string code) =>
        $"<p style=\"margin:4px 0 18px;padding:14px 0;background-color:{Page};border-radius:6px;text-align:center;" +
        $"font-family:Consolas,Menlo,monospace;font-size:28px;font-weight:bold;letter-spacing:6px;color:#18181b\">{Encode(code)}</p>";

    /// <summary>
    /// Link buttons in one row. Tables, because that's what Outlook draws reliably. Colours
    /// must already be valid hex colours (see <see cref="IAppBranding"/>).
    /// </summary>
    public static string Buttons(params EmailButton[] buttons) {
        var cells = buttons.Select(b =>
            $"<td bgcolor=\"{b.Background}\" style=\"border-radius:6px;background-color:{b.Background}\">" +
            $"<a href=\"{Encode(b.Url)}\" target=\"_blank\" " +
            $"style=\"display:inline-block;padding:11px 24px;border-radius:6px;{Font};" +
            $"font-size:15px;font-weight:bold;line-height:20px;color:{b.Foreground};text-decoration:none\">" +
            $"{Encode(b.Label)}</a></td>");

        return "<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"margin:8px 0 6px\"><tr>" +
               string.Join("<td style=\"width:12px\"></td>", cells) +
               "</tr></table>";
    }

    /// <summary>Escapes what HTML needs escaped; letters like é stay as they are (emails are UTF-8).</summary>
    private static string Encode(string text) => text
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("\"", "&quot;").Replace("'", "&#39;");
}

public record EmailButton(string Label, string Url, string Background, string Foreground);
