using InterOp.Server.Data;
using InterOp.Server.Domain;
using InterOp.Server.Dto;
using InterOp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InterOp.Server.Controllers;

[ApiController]
[Route("api/products/import/taobao")]
public class ProductsTaobaoController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly TaobaoService _taobao;           // Advanced (item_detail)
    private readonly TaobaoBasicService _taobaoBasic; // Basic   (seller list)

    public ProductsTaobaoController(
        AppDbContext db,
        IWebHostEnvironment env,
        TaobaoService taobao,
        TaobaoBasicService taobaoBasic)
    {
        _db = db;
        _env = env;
        _taobao = taobao;
        _taobaoBasic = taobaoBasic;
    }

    // ========== helpers ==========

    private static bool TryFindStringByKey(JsonElement e, string key, out string value)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in e.EnumerateObject())
            {
                if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value.ValueKind switch
                    {
                        JsonValueKind.String => p.Value.GetString() ?? "",
                        JsonValueKind.Number => p.Value.ToString(),
                        _ => ""
                    };
                    return true;
                }
                if (TryFindStringByKey(p.Value, key, out value)) return true;
            }
        }
        else if (e.ValueKind == JsonValueKind.Array)
        {
            foreach (var it in e.EnumerateArray())
                if (TryFindStringByKey(it, key, out value)) return true;
        }
        value = "";
        return false;
    }

    private static bool TryGetArrayFirstString(JsonElement e, string key, out string value)
    {
        if (e.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in e.EnumerateObject())
            {
                if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase) &&
                    p.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var it in p.Value.EnumerateArray())
                        if (it.ValueKind == JsonValueKind.String) { value = it.GetString()!; return true; }
                }
                if (TryGetArrayFirstString(p.Value, key, out value)) return true;
            }
        }
        else if (e.ValueKind == JsonValueKind.Array)
        {
            foreach (var it in e.EnumerateArray())
                if (TryGetArrayFirstString(it, key, out value)) return true;
        }
        value = "";
        return false;
    }

    private static bool TryFindFirstStringByKeys(JsonElement e, out string value, params string[] keys)
    {
        foreach (var k in keys)
            if (TryFindStringByKey(e, k, out value) && !string.IsNullOrWhiteSpace(value))
                return true;
        value = "";
        return false;
    }

    private static string FixUrl(string? u) =>
        string.IsNullOrWhiteSpace(u) ? "" :
        (u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? u : $"https:{u}");

    // Back-compat alias (da sve postojeće TryFind pozive ne moraš mijenjati)
    private static bool TryFind(JsonElement e, string key, out string val)
        => TryFindStringByKey(e, key, out val);

    private string XsdPath() => Path.Combine(_env.ContentRootPath, "XmlSchemas", "product.xsd");

    [HttpGet("xml/{numIid}")]
    public async Task<IActionResult> BuildXmlFromTaobao(
      string numIid,
      [FromQuery] string? sellerId = null,
      [FromQuery] int pages = 5,
      [FromQuery] int pageSize = 100,
      CancellationToken ct = default)
    {
        // --- helperi ---
        static bool TryFind(JsonElement e, string key, out string val)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in e.EnumerateObject())
                {
                    if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        val = p.Value.ValueKind switch
                        {
                            JsonValueKind.String => p.Value.GetString() ?? "",
                            JsonValueKind.Number => p.Value.ToString(),
                            _ => ""
                        };
                        return true;
                    }
                    if (TryFind(p.Value, key, out val)) return true;
                }
            }
            else if (e.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in e.EnumerateArray())
                    if (TryFind(it, key, out val)) return true;
            }
            val = ""; return false;
        }

        static string FixUrl(string? u) =>
            string.IsNullOrWhiteSpace(u) ? "" :
            (u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? u : $"https:{u}");

        // --- pokušaj 1: ADVANCED item_detail ---
        string? title = null, shopName = null, url = null, pic = null, sales = null, reviews = null, cat1 = null, cat2 = null;
        decimal? price = null;

        try
        {
            var payload = await _taobao.ItemDetailRawAsync(numIid, ct);
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (TryFind(root, "title", out var t) && !string.IsNullOrWhiteSpace(t)) title = t;
            if (TryFind(root, "shop_name", out var sn) && !string.IsNullOrWhiteSpace(sn)) shopName = sn;
            if (TryFind(root, "product_url", out var u) || TryFind(root, "detail_url", out u)) url = FixUrl(u);
            if (TryFind(root, "pic", out var p) || TryFind(root, "pic_url", out p) || TryFind(root, "main_pic", out p)) pic = FixUrl(p);
            if (TryFind(root, "sales", out var s)) sales = s;
            if (TryFind(root, "reviews", out var r)) reviews = r;
            if (TryFind(root, "cat_id", out var c1)) cat1 = c1;
            if (TryFind(root, "cat_id2", out var c2)) cat2 = c2;

            if (TryFind(root, "price", out var pr) &&
                decimal.TryParse(pr, NumberStyles.Any, CultureInfo.InvariantCulture, out var dn))
                price = dn;
        }
        catch { /* ignoriraj; ići ćemo na BASIC */ }

        // --- pokušaj 2: BASIC by-seller (samo ako imamo sellerId i još fali nešto) ---
        if (!string.IsNullOrWhiteSpace(sellerId) &&
            (string.IsNullOrWhiteSpace(title) || price is null || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(pic)))
        {
            string? lastSellerTitle = null;

            for (int page = 1; page <= pages; page++)
            {
                // VAŽNO: tvoj servis vraća (TaobaoRoot root, string payload)
                var (basicRoot, basicPayload) = await _taobaoBasic.GetShopItemsBySellerAsync(sellerId!, page, pageSize, ct);

                using var bdoc = JsonDocument.Parse(basicPayload);
                if (!bdoc.RootElement.TryGetProperty("result", out var result))
                    continue;

                if (result.TryGetProperty("seller_title", out var st) && st.ValueKind == JsonValueKind.String)
                    lastSellerTitle = st.GetString();

                if (!result.TryGetProperty("item", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                    continue;

                var hit = items.EnumerateArray()
                               .FirstOrDefault(it => it.TryGetProperty("num_iid", out var nid) && nid.ToString() == numIid);

                if (hit.ValueKind != JsonValueKind.Undefined)
                {
                    if (string.IsNullOrWhiteSpace(title) && hit.TryGetProperty("title", out var ht) && ht.ValueKind == JsonValueKind.String)
                        title = ht.GetString();

                    if (price is null && hit.TryGetProperty("price", out var hp))
                    {
                        if (hp.ValueKind == JsonValueKind.Number && hp.TryGetDecimal(out var pd)) price = pd;
                        else if (hp.ValueKind == JsonValueKind.String && decimal.TryParse(hp.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ps)) price = ps;
                    }

                    if (string.IsNullOrWhiteSpace(url) && hit.TryGetProperty("detail_url", out var du) && du.ValueKind == JsonValueKind.String)
                        url = FixUrl(du.GetString());
                    if (string.IsNullOrWhiteSpace(pic) && hit.TryGetProperty("pic", out var pi) && pi.ValueKind == JsonValueKind.String)
                        pic = FixUrl(pi.GetString());

                    if (string.IsNullOrWhiteSpace(sales) && hit.TryGetProperty("sales", out var sl) && sl.ValueKind == JsonValueKind.String) sales = sl.GetString();
                    if (string.IsNullOrWhiteSpace(reviews) && hit.TryGetProperty("reviews", out var rv) && rv.ValueKind == JsonValueKind.String) reviews = rv.GetString();
                    if (string.IsNullOrWhiteSpace(cat1) && hit.TryGetProperty("cat_id", out var c1v)) cat1 = c1v.ToString();
                    if (string.IsNullOrWhiteSpace(cat2) && hit.TryGetProperty("cat_id2", out var c2v)) cat2 = c2v.ToString();

                    break; // našli smo traženi item
                }
            }

            shopName ??= lastSellerTitle ?? "";
        }

        // --- sastavi XML ---
        var id = numIid;
        if (string.IsNullOrWhiteSpace(title)) title = $"Taobao item {id}";
        var priceStr = (price ?? 0m).ToString(CultureInfo.InvariantCulture);

        var xdoc = new XDocument(
            new XElement("Product",
                new XElement("Id", id),
                new XElement("Title", title),
                new XElement("Currency", "CNY"),
                new XElement("Price", priceStr),
                new XElement("ShopName", shopName ?? ""),
                new XElement("Url", url ?? ""),
                new XElement("Pic", pic ?? ""),
                new XElement("Sales", sales ?? ""),
                new XElement("Reviews", reviews ?? ""),
                new XElement("CategoryId", cat1 ?? ""),
                new XElement("CategoryId2", cat2 ?? "")
            )
        );

        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                  xdoc.ToString(SaveOptions.DisableFormatting);

        return Content(xml, "application/xml");
    }

    [HttpGet("_debug/advanced/{numIid}")]
    public async Task<IActionResult> DebugAdvanced(string numIid, CancellationToken ct = default)
    {
        try
        {
            var payload = await _taobao.ItemDetailRawAsync(numIid, ct);
            return Content(payload, "application/json");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("RapidAPI "))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

    [HttpGet("_debug/basic/by-seller/{sellerId}")]
    public async Task<IActionResult> DebugBasicBySeller(string sellerId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        try
        {
            var (root, payload) = await _taobaoBasic.GetShopItemsBySellerAsync(sellerId, page, pageSize, ct);
            return Content(payload, "application/json");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("RapidAPI "))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }
    }

}
