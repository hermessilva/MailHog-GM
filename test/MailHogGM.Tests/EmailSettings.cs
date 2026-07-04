namespace MailHogGM.Tests;

/// <summary>
/// Espelha o bloco "EmailSettings" usado pela aplicação sob teste para conectar
/// no SMTP. Aqui apontamos para o MailHog-GM (placebo do Gmail).
/// </summary>
public sealed class EmailSettings
{
    public string SmtpHost { get; init; } = "127.0.0.1";
    public int SmtpPort { get; init; } = 1025;
    public bool EnableSsl { get; init; } = true;
    public string SenderEmail { get; init; } = "no-reply@tootega.test";
    public string SenderName { get; init; } = "Tootega";
    public string Username { get; init; } = "test-user";
    public string Password { get; init; } = "test-pass";
    public int ResetTokenExpiryHours { get; init; } = 2;

    /// <summary>URL base da API HTTP do MailHog-GM (UI/API).</summary>
    public string ApiBaseUrl { get; init; } = "http://127.0.0.1:8025";

    /// <summary>Lê a configuração de variáveis de ambiente (usado no Docker).</summary>
    public static EmailSettings FromEnvironment() => new()
    {
        SmtpHost = Env("MAILHOG_SMTP_HOST", "127.0.0.1"),
        SmtpPort = int.Parse(Env("MAILHOG_SMTP_PORT", "1025")),
        EnableSsl = bool.Parse(Env("MAILHOG_ENABLE_SSL", "true")),
        Username = Env("MAILHOG_USERNAME", "test-user"),
        Password = Env("MAILHOG_PASSWORD", "test-pass"),
        ApiBaseUrl = Env("MAILHOG_API", "http://127.0.0.1:8025"),
    };

    private static string Env(string key, string fallback)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;
}
