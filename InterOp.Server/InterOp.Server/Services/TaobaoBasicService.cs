using System.Text.Json;
using System.Text.Json.Serialization;

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

    private (string Host, string Key) Cfg()
        => (_cfg["TaobaoBasic:Host"]!, _cfg["RapidApi:Key"]!);

    private async Task<(bool ok, string body, int status)> CallAsync(string url, CancellationToken ct)
    {
        var (host, key) = Cfg();

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-RapidAPI-Key", key);
        req.Headers.Add("X-RapidAPI-Host", host);

        using var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return (res.IsSuccessStatusCode, body, (int)res.StatusCode);
    }

    public async Task<(TaobaoRoot root, string payload)> GetShopItemsBySellerAsync(
     string sellerId, int page, int pageSize, CancellationToken ct)
    {
        var (host, _) = Cfg();
        var url = $"https://{host}/api?api=shop_items_by_seller&seller_id={Uri.EscapeDataString(sellerId)}&page={page}&page_size={pageSize}";
        var (ok, body, status) = await CallAsync(url, ct);

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        if (!ok)
        {
            return (new TaobaoRoot(), body);
        }

        var root = JsonSerializer.Deserialize<TaobaoRoot>(body, opts) ?? new TaobaoRoot();
        return (root, body);
    }

}
