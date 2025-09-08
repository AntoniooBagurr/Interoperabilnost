using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace InterOp.Server.Services
{
    public sealed class TaobaoService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;
        private readonly ILogger<TaobaoService> _log;

        public TaobaoService(HttpClient http, IConfiguration cfg, ILogger<TaobaoService> log)
        { _http = http; _cfg = cfg; _log = log; }

        private async Task<(bool ok, string body, int status)> CallAsync(string url, CancellationToken ct)
        {
            var (_, key) = Cfg();
            var hostOnly = NormalizeHost(_cfg["RapidApi:Host"]);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("X-RapidAPI-Key", key);
            req.Headers.Add("X-RapidAPI-Host", hostOnly);

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct); 
            return (res.IsSuccessStatusCode, body, (int)res.StatusCode);
        }


        private static string NormalizeHost(string? h)
        {
            if (string.IsNullOrWhiteSpace(h)) throw new InvalidOperationException("RapidApi:Host missing");
            h = h.Trim();
            if (h.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) h = h.Substring(8);
            if (h.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) h = h.Substring(7);
            return h.TrimEnd('/');
        }

        private (string Host, string Key) Cfg()
        {
            var host = NormalizeHost(_cfg["RapidApi:Host"]);
            var key = _cfg["RapidApi:Key"];
            if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("RapidApi:Key missing");
            return (host, key!);
        }


        public async Task<(TaobaoRoot root, string payload)> SearchAsync(string q, int page, int pageSize, CancellationToken ct)
        {
            var (host, _) = Cfg();
            var url = $"https://{host}/api?api=item_search&q={Uri.EscapeDataString(q)}&page={page}&page_size={pageSize}";
            var (ok, body, status) = await CallAsync(url, ct);
            if (!ok) throw new InvalidOperationException($"RapidAPI {status} for item_search. Body: {body}");

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString };
            var root = JsonSerializer.Deserialize<TaobaoRoot>(body, opts) ?? new TaobaoRoot();
            return (root, body);
        }

        public async Task<(TaobaoRoot root, string payload)> ItemDetailAsync(string numIid, CancellationToken ct)
        {
            var (host, _) = Cfg();
            var url = $"https://{host}/api?api=item_detail&num_iid={Uri.EscapeDataString(numIid)}";
            var (ok, body, status) = await CallAsync(url, ct);
            if (!ok) throw new InvalidOperationException($"RapidAPI {status} for item_detail. Body: {body}");

            var root = JsonSerializer.Deserialize<TaobaoRoot>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TaobaoRoot();
            return (root, body);
        }

        public async Task<string> ItemDetailRawAsync(string numIid, CancellationToken ct = default)
        {
            var (host, key) = Cfg();
            var url = $"https://{host}/api?api=item_detail&num_iid={Uri.EscapeDataString(numIid)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("X-RapidAPI-Key", key);
            req.Headers.Add("X-RapidAPI-Host", host);

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"RapidAPI {(int)res.StatusCode} {res.ReasonPhrase}. Body: {body}");
            return body;
        }
    }
}



