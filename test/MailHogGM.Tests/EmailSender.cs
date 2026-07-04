using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailHogGM.Tests;

/// <summary>
/// Envia e-mails para o MailHog-GM da mesma forma que a aplicação real faria
/// contra o Gmail: STARTTLS + AUTH LOGIN/PLAIN. Como o certificado do servidor
/// de teste é self-signed, a validação da cadeia é desabilitada (apenas teste).
/// </summary>
public sealed class EmailSender
{
    private readonly EmailSettings _settings;

    public EmailSender(EmailSettings settings) => _settings = settings;

    public async Task SendAsync(string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();

        // Cert self-signed de teste: aceita qualquer certificado.
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        client.CheckCertificateRevocation = false;

        var socketOptions = _settings.EnableSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOptions);
        // Modo placebo: qualquer credencial é aceita pelo MailHog-GM.
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
