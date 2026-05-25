// BarnaMu Web - EmailService.cs
// Sends transactional email through an SMTP relay (Brevo by default) using MailKit.
// When Smtp.Enabled is false the service is a no-op that logs the message body, so the
// site runs fine before SMTP credentials are configured.

using BarnaMu.Web.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BarnaMu.Web.Services;

public class EmailService
{
    private readonly BarnaMuOptions.SmtpOptions _smtp;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<BarnaMuOptions> options, ILogger<EmailService> logger)
    {
        this._smtp = options.Value.Smtp;
        this._logger = logger;
    }

    public bool IsEnabled => this._smtp.Enabled && !string.IsNullOrWhiteSpace(this._smtp.FromEmail);

    /// <summary>
    /// Sends an HTML email. Returns true if it was handed off to the relay, false if email
    /// is disabled or sending failed. Never throws — callers treat failure as "not sent".
    /// </summary>
    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (!this.IsEnabled)
        {
            this._logger.LogInformation(
                "Email disabled — would have sent to {To} | {Subject}\n{Body}", toEmail, subject, htmlBody);
            return false;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(this._smtp.FromName, this._smtp.FromEmail));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;
            msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(this._smtp.Host, this._smtp.Port, SecureSocketOptions.StartTls, ct)
                .ConfigureAwait(false);
            await client.AuthenticateAsync(this._smtp.Username, this._smtp.Password, ct).ConfigureAwait(false);
            await client.SendAsync(msg, ct).ConfigureAwait(false);
            await client.DisconnectAsync(true, ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to send email to {To}.", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Wraps content in the BarnaMu dark-fantasy email shell (matches the site theme).
    /// Email HTML uses tables + inline styles for client compatibility (Outlook, Gmail).
    /// </summary>
    public string BrandedHtml(string heading, string innerHtml)
    {
        var brand = System.Net.WebUtility.HtmlEncode(this._smtp.FromName);
        return $"""
            <!DOCTYPE html>
            <html lang="es">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background-color:#07090f;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#07090f;padding:32px 12px;">
                <tr><td align="center">
                  <table role="presentation" width="560" cellpadding="0" cellspacing="0" style="max-width:560px;width:100%;background-color:#0d1020;border:1px solid rgba(180,140,50,0.22);border-radius:14px;overflow:hidden;">
                    <tr><td style="padding:28px 36px 18px;text-align:center;border-bottom:1px solid rgba(180,140,50,0.22);">
                      <div style="font-family:Georgia,'Times New Roman',serif;font-size:24px;font-weight:bold;letter-spacing:2px;color:#e8b96a;">
                        <span style="color:#c8963c;">&#9876;</span>&nbsp;{brand}
                      </div>
                      <div style="font-family:Arial,Helvetica,sans-serif;font-size:11px;letter-spacing:3px;color:#65605a;text-transform:uppercase;margin-top:4px;">MU Online Season 6</div>
                    </td></tr>
                    <tr><td style="padding:30px 36px 34px;">
                      <h1 style="margin:0 0 18px;font-family:Georgia,'Times New Roman',serif;font-size:21px;font-weight:bold;color:#ede8d8;">{System.Net.WebUtility.HtmlEncode(heading)}</h1>
                      <div style="font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:1.6;color:#c4bfb2;">
                        {innerHtml}
                      </div>
                    </td></tr>
                    <tr><td style="padding:18px 36px 26px;border-top:1px solid rgba(180,140,50,0.12);text-align:center;font-family:Arial,Helvetica,sans-serif;font-size:11px;line-height:1.6;color:#65605a;">
                      {brand} &middot; MU Online Season 6 Episodio 3<br>
                      Servidor privado &mdash; no afiliado a Webzen.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>Renders a gold call-to-action button for branded emails.</summary>
    public static string Button(string text, string url)
    {
        var safeUrl = System.Net.WebUtility.HtmlEncode(url);
        var safeText = System.Net.WebUtility.HtmlEncode(text);
        return $"""
            <table role="presentation" cellpadding="0" cellspacing="0" style="margin:24px auto;">
              <tr><td style="border-radius:8px;background:linear-gradient(180deg,#e8b96a,#c8963c);">
                <a href="{safeUrl}" style="display:inline-block;padding:13px 30px;font-family:Georgia,'Times New Roman',serif;font-size:15px;font-weight:bold;letter-spacing:0.5px;color:#1a1206;text-decoration:none;border-radius:8px;">{safeText}</a>
              </td></tr>
            </table>
            """;
    }
}
