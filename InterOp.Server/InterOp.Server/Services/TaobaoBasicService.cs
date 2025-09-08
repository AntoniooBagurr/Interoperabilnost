using System.Net.Http;
using Microsoft.Extensions.Configuration;

namespace InterOp.Server.Services;

public sealed class TaobaoBasicService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public TaobaoBasicService(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }


    public readonly record struct BasicResponse(bool ok, string payload, int status);

    private static string NormalizeHost(string? h)
    {
        if (string.IsNullOrWhiteSpace(h)) return "localhost:7205"; 
        h = h.Trim();
        if (h.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) h = h[8..];
        if (h.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) h = h[7..];
        return h.TrimEnd('/');
    }

    public async Task<BasicResponse> GetShopItemsBySellerAsync(
        string sellerId, int page, int pageSize, CancellationToken ct = default)
    {
        var host = NormalizeHost(_cfg["TaobaoBasic:Host"]); 
        var url = $"https://{host}/api?api=shop_items&seller_id={Uri.EscapeDataString(sellerId)}&page={page}&page_size={pageSize}";

        using var res = await _http.GetAsync(url, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return new BasicResponse(res.IsSuccessStatusCode, body, (int)res.StatusCode);
    }
}
