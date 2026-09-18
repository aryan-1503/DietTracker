using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DietTracker.API.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string token, string? displayName = null, CancellationToken ct = default);
    Task SendReminderEmailAsync(string toEmail, string displayName, DateOnly date, int completedSlots, int totalSlots, CancellationToken ct = default);
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
        var appUrl  = _config.GetValue<string>("AppUrl")  ?? "http://localhost:4200";
        var appName = _config.GetValue<string>("AppName") ?? "EatTrack";

        var verifyLink = $"{appUrl}/auth/verify-email?token={Uri.EscapeDataString(token)}";
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            smtp["SenderName"] ?? appName,
            smtp["SenderEmail"] ?? smtp["Username"] ?? $"no-reply@{appName.ToLower()}.app"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"Verify your {appName} email";

        var body = new BodyBuilder
        {
            HtmlBody = BuildHtmlEmail(verifyLink, token, greeting, appName),
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

    public async Task SendReminderEmailAsync(string toEmail, string displayName, DateOnly date, int completedSlots, int totalSlots, CancellationToken ct = default)
    {
        var smtp = _config.GetSection("SmtpSettings");
        var appUrl    = _config.GetValue<string>("AppUrl")  ?? "http://localhost:4200";
        var appName   = _config.GetValue<string>("AppName") ?? "EatTrack";
        var dailyIntakeLink = $"{appUrl}/daily-intake";
        var greeting = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var dateStr  = date.ToString("dddd, MMMM d, yyyy");
        var remaining = totalSlots - completedSlots;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            smtp["SenderName"] ?? appName,
            smtp["SenderEmail"] ?? smtp["Username"] ?? $"no-reply@{appName.ToLower()}.app"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"📋 {appName} reminder — {remaining} meal{(remaining == 1 ? "" : "s")} left for today";

        var body = new BodyBuilder
        {
            HtmlBody = BuildReminderHtml(greeting, dateStr, completedSlots, totalSlots, remaining, dailyIntakeLink, appName),
            TextBody = $"Hi {greeting},\n\nYou have {remaining} meal slot{(remaining == 1 ? "" : "s")} left to log for {dateStr}.\n" +
                       $"Completed: {completedSlots}/{totalSlots}.\n\nLog your meals here: {dailyIntakeLink}",
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

        _logger.LogInformation("Reminder email sent to {Email} for date {Date}", toEmail, date);
    }

    private static string BuildReminderHtml(string greeting, string dateStr, int completed, int total, int remaining, string dailyIntakeLink, string appName) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f5f7fa;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="padding:32px 16px;">
            <tr><td align="center">
              <table width="100%" style="max-width:480px;background:#fff;border-radius:16px;padding:40px 32px;box-shadow:0 2px 16px rgba(0,0,0,0.07);" cellpadding="0" cellspacing="0">
                <tr><td align="center" style="padding-bottom:8px;font-size:2rem;">📋</td></tr>
                <tr><td align="center" style="padding-bottom:4px;">
                  <h1 style="margin:0;font-size:22px;font-weight:700;color:#1a1a2e;">Meal log reminder</h1>
                </td></tr>
                <tr><td align="center" style="padding:8px 0 20px;color:#6b7280;font-size:15px;line-height:1.5;">
                  Hi {greeting}! You have <strong>{remaining} meal slot{(remaining == 1 ? "" : "s")}</strong> left to log for <strong>{dateStr}</strong>.
                </td></tr>
                <tr><td align="center" style="padding-bottom:28px;">
                  <div style="background:#f3f4f6;border-radius:12px;padding:16px 24px;display:inline-block;text-align:center;">
                    <span style="font-size:28px;font-weight:700;color:#4f46e5;">{completed}/{total}</span>
                    <div style="font-size:13px;color:#9ca3af;margin-top:4px;">meals completed</div>
                  </div>
                </td></tr>
                <tr><td align="center" style="padding-bottom:28px;">
                  <a href="{dailyIntakeLink}" style="display:inline-block;background:#4f46e5;color:#fff;text-decoration:none;padding:14px 36px;border-radius:12px;font-size:16px;font-weight:600;">
                    Log your meals →
                  </a>
                </td></tr>
                <tr><td align="center" style="color:#d1d5db;font-size:12px;">
                  If the button doesn't work, copy this link: {dailyIntakeLink}
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    private static string BuildHtmlEmail(string verifyLink, string token, string greeting, string appName) => $"""
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
                  Hi {greeting}! Tap the button below to confirm your email address and start using {appName}.
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
                  This link expires in 24 hours. If you didn't create a {appName} account, you can ignore this email.
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
