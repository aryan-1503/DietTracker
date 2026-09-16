using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DietTracker.API.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string token, string? displayName = null, CancellationToken ct = default);
}

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string token, string? displayName = null, CancellationToken ct = default)
    {
        var smtp = _config.GetSection("SmtpSettings");
        var appUrl = _config.GetValue<string>("AppUrl") ?? "http://localhost:4200";

        var verifyLink = $"{appUrl}/auth/verify-email?token={Uri.EscapeDataString(token)}";
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            smtp["SenderName"] ?? "DietTracker",
            smtp["SenderEmail"] ?? smtp["Username"] ?? "no-reply@diettracker.app"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Verify your DietTracker email";

        var body = new BodyBuilder
        {
            HtmlBody = BuildHtmlEmail(verifyLink, token, greeting),
            TextBody = $"Hi {greeting},\n\nVerify your email by visiting:\n{verifyLink}\n\nOr paste this token: {token}",
        };
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();

        var host = smtp["Host"] ?? throw new InvalidOperationException("SmtpSettings:Host is not configured.");
        var port = int.Parse(smtp["Port"] ?? "587");
        var useSsl = bool.Parse(smtp["UseSsl"] ?? "false");
        var socketOptions = useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(host, port, socketOptions, ct);

        var user = smtp["Username"];
        var pass = smtp["Password"];
        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            await client.AuthenticateAsync(user, pass, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Verification email sent to {Email}", toEmail);
    }

    private static string BuildHtmlEmail(string verifyLink, string token, string greeting) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f5f7fa;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="padding:32px 16px;">
            <tr><td align="center">
              <table width="100%" style="max-width:480px;background:#fff;border-radius:16px;padding:40px 32px;box-shadow:0 2px 16px rgba(0,0,0,0.07);" cellpadding="0" cellspacing="0">
                <tr><td align="center" style="padding-bottom:8px;font-size:2rem;">🥗</td></tr>
                <tr><td align="center" style="padding-bottom:4px;">
                  <h1 style="margin:0;font-size:22px;font-weight:700;color:#1a1a2e;">Verify your email</h1>
                </td></tr>
                <tr><td align="center" style="padding:8px 0 28px;color:#6b7280;font-size:15px;line-height:1.5;">
                  Hi {greeting}! Tap the button below to confirm your email address and start using DietTracker.
                </td></tr>
                <tr><td align="center" style="padding-bottom:28px;">
                  <a href="{verifyLink}" style="display:inline-block;background:#4f46e5;color:#fff;text-decoration:none;padding:14px 36px;border-radius:12px;font-size:16px;font-weight:600;">
                    Verify email
                  </a>
                </td></tr>
                <tr><td align="center" style="padding-bottom:8px;color:#9ca3af;font-size:13px;">
                  Or paste this token on the verification page:
                </td></tr>
                <tr><td align="center" style="padding-bottom:28px;">
                  <code style="display:inline-block;background:#f3f4f6;border-radius:8px;padding:10px 18px;font-size:13px;word-break:break-all;color:#1a1a2e;">{token}</code>
                </td></tr>
                <tr><td align="center" style="color:#d1d5db;font-size:12px;">
                  This link expires in 24 hours. If you didn't create a DietTracker account, you can ignore this email.
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
