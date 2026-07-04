using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MailHogGM.Tests;

/// <summary>Cliente da API HTTP do MailHog-GM (recebimento/busca de e-mails).</summary>
public sealed class MailHogApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public MailHogApiClient(string baseUrl)
        => _http = new HttpClient { BaseAddress = new Uri(baseUrl) };

    /// <summary>Aguarda a API ficar disponível (usado ao subir o container).</summary>
    public async Task WaitUntilReadyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var res = await _http.GetAsync("/api/v2/messages");
                if (res.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
                // servidor ainda subindo
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"MailHog-GM API não respondeu em {timeout}.");
    }

    /// <summary>Apaga todas as mensagens (isola cada teste).</summary>
    public async Task DeleteAllAsync()
    {
        using var res = await _http.DeleteAsync("/api/v1/messages");
        res.EnsureSuccessStatusCode();
    }

    /// <summary>Lista as mensagens; a API retorna o mais novo primeiro.</summary>
    public async Task<IReadOnlyList<ApiMessage>> ListAsync()
    {
        var result = await _http.GetFromJsonAsync<ApiList>("/api/v2/messages", JsonOptions);
        return result?.Items ?? new List<ApiMessage>();
    }

    /// <summary>Faz polling até haver ao menos <paramref name="expected"/> mensagens.</summary>
    public async Task<IReadOnlyList<ApiMessage>> WaitForMessagesAsync(int expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var messages = await ListAsync();
            if (messages.Count >= expected)
                return messages;
            await Task.Delay(200);
        }

        throw new TimeoutException($"Esperava {expected} mensagem(ns) mas não chegaram em {timeout}.");
    }
}

public sealed record ApiList(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("start")] int Start,
    [property: JsonPropertyName("items")] List<ApiMessage> Items);

public sealed record ApiMessage(
    [property: JsonPropertyName("Content")] ApiContent Content,
    [property: JsonPropertyName("Created")] DateTime Created)
{
    public string Subject =>
        Content.Headers.TryGetValue("Subject", out var v) && v.Count > 0 ? v[0] : string.Empty;

    public string Body => Content.Body;
}

public sealed record ApiContent(
    [property: JsonPropertyName("Headers")] Dictionary<string, List<string>> Headers,
    [property: JsonPropertyName("Body")] string Body);
