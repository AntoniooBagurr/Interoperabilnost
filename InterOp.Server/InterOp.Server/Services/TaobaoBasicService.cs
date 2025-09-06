using System.Text.Json;

namespace InterOp.Server.Services
{
    public sealed class TaobaoBasicService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;
        public TaobaoBasicService(HttpClient http, IConfiguration cfg) { _http = http; _cfg = cfg; }

        private static string Norm(string? h)
        {
            if (string.IsNullOrWhiteSpace(h)) throw new InvalidOperationException("TaobaoBasic:Host missing");
            h = h.Trim();
            if (h.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) h = h[8..];
            if (h.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) h = h[7..];
            return h.TrimEnd('/');
        }

        public async Task<(bool ok, int status, string payload)> ShopItemsBySellerAsync(string sellerId, int page, int pageSize, CancellationToken ct)
        {
            var host = Norm(_cfg["TaobaoBasic:Host"]);
            var key = _cfg["RapidApi:Key"] ?? throw new InvalidOperationException("RapidApi:Key missing");
            var url = $"https://{host}/shop_items?seller_id={Uri.EscapeDataString(sellerId)}&page={page}&page_size={pageSize}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("X-RapidAPI-Key", key);
            req.Headers.Add("X-RapidAPI-Host", host);

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            return (res.IsSuccessStatusCode, (int)res.StatusCode, body);
        }
    }
}
