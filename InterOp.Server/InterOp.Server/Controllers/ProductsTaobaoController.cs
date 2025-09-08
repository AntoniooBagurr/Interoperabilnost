using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using InterOp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace InterOp.Server.Controllers;

[ApiController]
[Route("api/products/import/taobao")]
public class ProductsTaobaoController : ControllerBase
{
    private readonly TaobaoService _taobao;              
    private readonly TaobaoBasicService _taobaoBasic;    

    public ProductsTaobaoController(TaobaoService taobao, TaobaoBasicService taobaoBasic)
    {
        _taobao = taobao;
        _taobaoBasic = taobaoBasic;
    }


    static string FixUrl(string? u) =>
        string.IsNullOrWhiteSpace(u) ? "" :
        (u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? u : $"https:{u}");

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

    [HttpGet("xml/{numIid}")]
    public async Task<IActionResult> BuildXmlFromTaobao(
        string numIid,
        [FromQuery] string? sellerId = null,
        [FromQuery] int pages = 3,
        [FromQuery] int pageSize = 60,
        CancellationToken ct = default)
    {
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
        catch { }

        if (!string.IsNullOrWhiteSpace(sellerId) &&
            (string.IsNullOrWhiteSpace(title) || price is null || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(pic)))
        {
            for (int page = 1; page <= pages; page++)
            {
                var basic = await _taobaoBasic.GetShopItemsBySellerAsync(sellerId!, page, pageSize, ct);
                if (!basic.ok) break;

                using var bdoc = JsonDocument.Parse(basic.payload);
                if (!bdoc.RootElement.TryGetProperty("result", out var result)) continue;

                if (string.IsNullOrWhiteSpace(shopName) &&
                    result.TryGetProperty("seller_title", out var st) && st.ValueKind == JsonValueKind.String)
                    shopName = st.GetString();

                if (!result.TryGetProperty("item", out var items) || items.ValueKind != JsonValueKind.Array) continue;

                foreach (var it in items.EnumerateArray())
                {
                    if (!it.TryGetProperty("num_iid", out var nid)) continue;
                    if (!string.Equals(nid.ToString(), numIid, StringComparison.Ordinal)) continue;

                    if (string.IsNullOrWhiteSpace(title) && it.TryGetProperty("title", out var tt) && tt.ValueKind == JsonValueKind.String)
                        title = tt.GetString();

                    if (price is null && it.TryGetProperty("price", out var pr))
                    {
                        if (pr.ValueKind == JsonValueKind.Number && pr.TryGetDecimal(out var pd)) price = pd;
                        else if (pr.ValueKind == JsonValueKind.String && decimal.TryParse(pr.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ps)) price = ps;
                    }

                    if (string.IsNullOrWhiteSpace(url) && it.TryGetProperty("detail_url", out var du) && du.ValueKind == JsonValueKind.String)
                        url = FixUrl(du.GetString());
                    if (string.IsNullOrWhiteSpace(pic) && it.TryGetProperty("pic", out var pi) && pi.ValueKind == JsonValueKind.String)
                        pic = FixUrl(pi.GetString());
                    if (string.IsNullOrWhiteSpace(sales) && it.TryGetProperty("sales", out var sl) && sl.ValueKind == JsonValueKind.String)
                        sales = sl.GetString();
                    if (string.IsNullOrWhiteSpace(reviews) && it.TryGetProperty("reviews", out var rv) && rv.ValueKind == JsonValueKind.String)
                        reviews = rv.GetString();

                    if (string.IsNullOrWhiteSpace(cat1) && it.TryGetProperty("cat_id", out var c1v)) cat1 = c1v.ToString();
                    if (string.IsNullOrWhiteSpace(cat2) && it.TryGetProperty("cat_id2", out var c2v)) cat2 = c2v.ToString();

                    break;
                }

                if (!string.IsNullOrWhiteSpace(title) && price is not null && !string.IsNullOrWhiteSpace(url))
                    break;
            }
        }

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
        return Content(xml, "application/xml", Encoding.UTF8);
    }
}
