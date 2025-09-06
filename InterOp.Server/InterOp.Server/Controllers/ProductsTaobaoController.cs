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

namespace InterOp.Server.Controllers
{
    [ApiController]
    [Route("api/products/import/taobao")]
    public class ProductsTaobaoController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly TaobaoService _taobao;            // advanced host (item_detail)
        private readonly TaobaoBasicService _taobaoBasic;  // basic host (shop_items)

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

        // ----------------- helpers -----------------
        private static bool TryFindStringByKey(JsonElement e, string key, out string value)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                foreach (var p in e.EnumerateObject())
                {
                    if (string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase))
                    {
                        if (p.Value.ValueKind == JsonValueKind.String) { value = p.Value.GetString()!; return true; }
                        if (p.Value.ValueKind == JsonValueKind.Number) { value = p.Value.ToString(); return true; }
                    }
                    if (TryFindStringByKey(p.Value, key, out value)) return true;
                }
            }
            else if (e.ValueKind == JsonValueKind.Array)
            {
                foreach (var it in e.EnumerateArray())
                    if (TryFindStringByKey(it, key, out value)) return true;
            }
            value = ""; return false;
        }

        private static bool TryFindFirstStringByKeys(JsonElement e, out string value, params string[] keys)
        {
            foreach (var k in keys)
                if (TryFindStringByKey(e, k, out value) && !string.IsNullOrWhiteSpace(value))
                    return true;
            value = ""; return false;
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
            value = ""; return false;
        }

        private static bool TryFindDecimalByKeys(JsonElement e, out decimal result, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (TryFindStringByKey(e, k, out var s) && !string.IsNullOrWhiteSpace(s) &&
                    decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                    return true;
            }
            result = 0m; return false;
        }

        private static string FixUrl(string? u) =>
            string.IsNullOrWhiteSpace(u) ? "" :
            (u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? u : $"https:{u}");

        private string XsdPath() => Path.Combine(_env.ContentRootPath, "XmlSchemas", "product.xsd");

        // ============================================================
        // 1) IMPORT PREKO ITEM DETAIL (ADVANCED HOST)
        // ============================================================
        [HttpPost("by-detail/{numIid}")]
        public async Task<IActionResult> ImportByDetail(string numIid, CancellationToken ct = default)
        {
            try
            {
                var payload = await _taobao.ItemDetailRawAsync(numIid, ct);

                using var doc = JsonDocument.Parse(payload);
                var rootEl = doc.RootElement;

                // Id
                TryFindFirstStringByKeys(rootEl, out var id, "item_id", "num_iid", "id");
                if (string.IsNullOrWhiteSpace(id)) id = numIid;

                // Title
                if (!TryFindFirstStringByKeys(rootEl, out var title, "title", "subject", "name"))
                    title = $"Taobao item {id}";

                // Price
                if (!TryFindDecimalByKeys(rootEl, out var price, "price", "current_price", "reserve_price", "final_price", "min_price", "price_info"))
                    price = 0m;

                // Pic (best-effort)
                string pic = "";
                if (!TryFindFirstStringByKeys(rootEl, out pic, "pic", "pic_url", "main_pic", "image", "img", "cover"))
                    TryGetArrayFirstString(rootEl, "images", out pic);

                // Shop
                if (!TryFindFirstStringByKeys(rootEl, out var shopName, "shop_name", "seller_title", "shop_title", "nick"))
                    shopName = "";

                // Url
                if (!TryFindFirstStringByKeys(rootEl, out var url, "product_url", "detail_url", "url"))
                    url = $"https://item.taobao.com/item.htm?id={id}";
                url = FixUrl(url);

                // XML (redoslijed = XSD)
                var xmlDoc = new XDocument(
                    new XElement("Product",
                        new XElement("Id", id),
                        new XElement("Title", title),
                        new XElement("Currency", "CNY"),
                        new XElement("Price", price.ToString(CultureInfo.InvariantCulture)),
                        new XElement("ShopName", shopName),
                        new XElement("Url", url),
                        new XElement("Pic", pic)                    // ostala polja detail obično nema
                    )
                );
                var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + xmlDoc.ToString(SaveOptions.DisableFormatting);

                // XSD
                await using var xmlMs = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                await using var xsd = System.IO.File.OpenRead(XsdPath());
                var (ok, errors) = XmlValidationService.ValidateWithXsd(xmlMs, xsd);
                if (!ok) return UnprocessableEntity(new { message = "XSD validation failed", errors });

                // Deserialize -> save (SADA mapiramo i dodatna polja!)
                xmlMs.Position = 0;
                var ser = new XmlSerializer(typeof(ProductXml));
                if (ser.Deserialize(xmlMs) is not ProductXml dto)
                    return BadRequest(new { message = "XML deserialization failed" });

                if (await _db.Products.AnyAsync(p => p.ExtId == dto.Id, ct))
                    return Conflict(new { message = "Product već postoji.", dto.Id });

                _db.Products.Add(new Product
                {
                    ExtId = dto.Id,
                    Title = dto.Title,
                    Currency = dto.Currency,
                    Price = dto.Price,
                    ShopName = dto.ShopName,
                    Url = dto.Url,
                    Pic = dto.Pic,                      // <-- NOVO
                    Sales = dto.Sales,                  // <-- NOVO
                    Reviews = dto.Reviews,              // <-- NOVO
                    CategoryId = dto.CategoryId,        // <-- NOVO
                    CategoryId2 = dto.CategoryId2       // <-- NOVO
                });
                await _db.SaveChangesAsync(ct);

                var entity = await _db.Products.OrderByDescending(p => p.Id).FirstAsync(ct);
                return Created($"/api/products/{entity.Id}", entity);
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("RapidAPI "))
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
            }
        }

        // ============================================================
        // 2) IMPORT PREKO SHOP_ITEMS (BASIC HOST) – vraća price/pic/...
        // ============================================================
        [HttpPost("basic/by-seller/{sellerId}")]
        public async Task<IActionResult> ImportBasicBySeller(
     string sellerId,
     int take = 1,
     int page = 1,
     int pageSize = 60,          // 60 da pokrije sve artikle iz shopa
     string? numIid = null,      // <-- NOVO: filtriraj po id-u
     CancellationToken ct = default)
        {
            var res = await _taobaoBasic.ShopItemsBySellerAsync(sellerId, page, pageSize, ct);
            if (!res.ok) return StatusCode(StatusCodes.Status502BadGateway, new { message = $"RapidAPI {res.status}", res.payload });

            using var doc = JsonDocument.Parse(res.payload);
            if (!doc.RootElement.TryGetProperty("result", out var result))
                return BadRequest(new { message = "Nepoznat odgovor", res.payload });

            var sellerTitle = result.TryGetProperty("seller_title", out var st) && st.ValueKind == JsonValueKind.String ? st.GetString()! : "";

            if (!result.TryGetProperty("item", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
                return NotFound(new { message = "Nema artikala za danog sellera.", sellerId, res.payload });

            // ---- FILTER PO numIid (ako je zadano) ----
            var seq = items.EnumerateArray();
            if (!string.IsNullOrWhiteSpace(numIid))
                seq = (JsonElement.ArrayEnumerator)seq.Where(it => it.TryGetProperty("num_iid", out var nid) && nid.ToString() == numIid);

            var list = seq.Take(take <= 0 ? int.MaxValue : take).ToList();
            if (list.Count == 0)
                return NotFound(new { message = "Nema artikala koji odgovaraju numIid.", sellerId, numIid });

            var xsdPath = Path.Combine(_env.ContentRootPath, "XmlSchemas", "product.xsd");
            var ser = new XmlSerializer(typeof(ProductXml));
            int created = 0, skipped = 0, failed = 0; var results = new List<object>();

            foreach (var it in list)
            {
                var id = it.GetProperty("num_iid").ToString();
                var title = it.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString()! : $"Taobao item {id}";

                decimal price = 0m;
                if (it.TryGetProperty("price", out var p))
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var pd)) price = pd;
                    else if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ps)) price = ps;
                }

                var url = it.TryGetProperty("detail_url", out var du) && du.ValueKind == JsonValueKind.String ? du.GetString()! : $"https://item.taobao.com/item.htm?id={id}";
                var pic = it.TryGetProperty("pic", out var pi) && pi.ValueKind == JsonValueKind.String ? pi.GetString()! : "";
                var sales = it.TryGetProperty("sales", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString()! : "";
                var reviews = it.TryGetProperty("reviews", out var rv) && rv.ValueKind == JsonValueKind.String ? rv.GetString()! : "";
                var catId = it.TryGetProperty("cat_id", out var c1) ? c1.ToString() : "";
                var catId2 = it.TryGetProperty("cat_id2", out var c2) ? c2.ToString() : "";

                var xmlDoc = new XDocument(
                    new XElement("Product",
                        new XElement("Id", id),
                        new XElement("Title", title),
                        new XElement("Currency", "CNY"),
                        new XElement("Price", price.ToString(CultureInfo.InvariantCulture)),
                        new XElement("ShopName", sellerTitle),
                        new XElement("Url", url),
                        new XElement("Pic", pic),
                        new XElement("Sales", sales),
                        new XElement("Reviews", reviews),
                        new XElement("CategoryId", catId),
                        new XElement("CategoryId2", catId2)
                    )
                );
                var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + xmlDoc.ToString(SaveOptions.DisableFormatting);

                await using var xmlMs = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                await using var xsd = System.IO.File.OpenRead(xsdPath);
                var (ok, errors) = XmlValidationService.ValidateWithXsd(xmlMs, xsd);
                if (!ok) { failed++; results.Add(new { id, status = "invalid", errors }); continue; }

                xmlMs.Position = 0;
                if (ser.Deserialize(xmlMs) is not ProductXml dto) { failed++; results.Add(new { id, status = "deserialize_failed" }); continue; }

                if (await _db.Products.AnyAsync(p => p.ExtId == dto.Id, ct)) { skipped++; results.Add(new { id, status = "exists" }); continue; }

                _db.Products.Add(new Product
                {
                    ExtId = dto.Id,
                    Title = dto.Title,
                    Currency = dto.Currency,
                    Price = dto.Price,
                    ShopName = dto.ShopName,
                    Url = dto.Url,
                    Pic = dto.Pic,
                    Sales = dto.Sales,
                    Reviews = dto.Reviews,
                    CategoryId = dto.CategoryId,
                    CategoryId2 = dto.CategoryId2
                });
                await _db.SaveChangesAsync(ct);

                created++; results.Add(new { id, status = "created" });
            }

            return Ok(new { created, skipped, failed, results });
        }


        // ============================================================
        // 3) IMPORT PRETRAGOM (ADVANCED HOST; zahtijeva plan)
        // ============================================================
        [HttpPost("by-search")]
        public async Task<IActionResult> ImportBySearch([FromQuery] string q = "ssd", int take = 1, int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            try
            {
                var (search, raw) = await _taobao.SearchAsync(q, page, pageSize, ct);
                var items = search.result.item ?? new();
                if (items.Count == 0)
                    return NotFound(new { message = "Nema artikala za dani upit.", q, payload = raw });

                var toImport = (take <= 0 ? items : items.Take(take)).ToList();

                var xsdPath = XsdPath();
                var ser = new XmlSerializer(typeof(ProductXml));
                var results = new List<object>(); int created = 0, skipped = 0, failed = 0;

                foreach (var it in toImport)
                {
                    var xmlDoc = new XDocument(
                        new XElement("Product",
                            new XElement("Id", it.num_iid.ToString()),
                            new XElement("Title", it.title),
                            new XElement("Currency", "CNY"),
                            new XElement("Price", (it.price ?? 0m).ToString(CultureInfo.InvariantCulture)),
                            new XElement("ShopName", search.result.seller_title ?? ""),
                            new XElement("Url", FixUrl(it.detail_url))
                        )
                    );
                    var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + xmlDoc.ToString(SaveOptions.DisableFormatting);

                    await using var xmlMs = new MemoryStream(Encoding.UTF8.GetBytes(xml));
                    await using var xsd = System.IO.File.OpenRead(xsdPath);
                    var (ok, errors) = XmlValidationService.ValidateWithXsd(xmlMs, xsd);
                    if (!ok) { failed++; results.Add(new { it.num_iid, status = "invalid", errors }); continue; }

                    xmlMs.Position = 0;
                    if (ser.Deserialize(xmlMs) is not ProductXml dto) { failed++; results.Add(new { it.num_iid, status = "deserialize_failed" }); continue; }

                    if (await _db.Products.AnyAsync(p => p.ExtId == dto.Id, ct)) { skipped++; results.Add(new { it.num_iid, status = "exists" }); continue; }

                    _db.Products.Add(new Product
                    {
                        ExtId = dto.Id,
                        Title = dto.Title,
                        Currency = dto.Currency,
                        Price = dto.Price,
                        ShopName = dto.ShopName,
                        Url = dto.Url,
                        Pic = dto.Pic,
                        Sales = dto.Sales,
                        Reviews = dto.Reviews,
                        CategoryId = dto.CategoryId,
                        CategoryId2 = dto.CategoryId2
                    });
                    await _db.SaveChangesAsync(ct);
                    created++; results.Add(new { it.num_iid, status = "created" });
                }

                return Ok(new { created, skipped, failed, results });
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("RapidAPI "))
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
            }
        }

        // ---------- GET ----------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.Products.FindAsync(id);
            return item is null ? NotFound() : Ok(item);
        }
    }
}
