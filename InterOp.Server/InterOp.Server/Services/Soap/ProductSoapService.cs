using InterOp.Server.Dto;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.XPath;

namespace InterOp.Server.Services.Soap
{
    public sealed class ProductSoapService : IProductSoapService
    {
        private readonly TaobaoBasicService _basic;
        private readonly SoapOptions _opt;

        public ProductSoapService(TaobaoBasicService basic, IOptions<SoapOptions> opt)
        {
            _basic = basic;
            _opt = opt.Value ?? new SoapOptions();
        }

        public SoapSearchResponse Search(string term, string? sellerId, int pages, int pageSize)
        {
            var xml = BuildXml(
                sellerId: string.IsNullOrWhiteSpace(sellerId) ? _opt.SellerId : sellerId!,
                pages: pages > 0 ? pages : _opt.Pages,
                pageSize: pageSize > 0 ? pageSize : _opt.PageSize);

            var lit = XPathLiteral(term ?? "");
            var xpath =
                $"/Products/Product[" +
                $"contains(Id, {lit}) or " +
                $"contains(Title, {lit}) or " +
                $"contains(ShopName, {lit}) or " +
                $"contains(CategoryId, {lit}) or " +
                $"contains(CategoryId2, {lit})" +
                $"]";

            var filtered = xml.XPathSelectElements(xpath).ToList();

            var items = filtered.Select(e => new SoapProduct
            {
                Id = (string?)e.Element("Id") ?? "",
                Title = (string?)e.Element("Title") ?? "",
                Currency = (string?)e.Element("Currency") ?? "CNY",
                Price = decimal.TryParse((string?)e.Element("Price"),
                          NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m,
                ShopName = (string?)e.Element("ShopName") ?? "",
                Url = (string?)e.Element("Url") ?? "",
                Pic = (string?)e.Element("Pic") ?? "",
                Sales = (string?)e.Element("Sales") ?? "",
                Reviews = (string?)e.Element("Reviews") ?? "",
                CategoryId = (string?)e.Element("CategoryId") ?? "",
                CategoryId2 = (string?)e.Element("CategoryId2") ?? ""
            }).ToArray();

            return new SoapSearchResponse
            {
                Count = items.Length,
                Items = items,
                RawXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + xml.ToString(SaveOptions.DisableFormatting)
            };
        }

        // ---------------- helpers ----------------
        private XDocument BuildXml(string sellerId, int pages, int pageSize)
        {
            var root = new XElement("Products");
            string sellerTitle = "";

            for (int page = 1; page <= pages; page++)
            {
                var response = _basic.GetShopItemsBySellerAsync(sellerId, page, pageSize).GetAwaiter().GetResult();
                if (!response.ok) break;

                using var doc = JsonDocument.Parse(response.payload);
                if (!doc.RootElement.TryGetProperty("result", out var result)) continue;

                if (string.IsNullOrWhiteSpace(sellerTitle) &&
                    result.TryGetProperty("seller_title", out var st) &&
                    st.ValueKind == JsonValueKind.String)
                    sellerTitle = st.GetString() ?? "";

                if (!result.TryGetProperty("item", out var items) || items.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var it in items.EnumerateArray())
                {
                    var id = it.TryGetProperty("num_iid", out var nid) ? nid.ToString() : "";
                    var title = it.TryGetProperty("title", out var tt) && tt.ValueKind == JsonValueKind.String ? tt.GetString() ?? "" : "";
                    var priceStr = it.TryGetProperty("price", out var pr)
                        ? (pr.ValueKind == JsonValueKind.Number ? pr.ToString() : pr.GetString() ?? "0")
                        : "0";
                    var url = it.TryGetProperty("detail_url", out var du) && du.ValueKind == JsonValueKind.String ? FixUrl(du.GetString()) : "";
                    var pic = it.TryGetProperty("pic", out var pi) && pi.ValueKind == JsonValueKind.String ? FixUrl(pi.GetString()) : "";
                    var sales = it.TryGetProperty("sales", out var sl) && sl.ValueKind == JsonValueKind.String ? sl.GetString() ?? "" : "";
                    var reviews = it.TryGetProperty("reviews", out var rv) && rv.ValueKind == JsonValueKind.String ? rv.GetString() ?? "" : "";
                    var cat1 = it.TryGetProperty("cat_id", out var c1) ? c1.ToString() : "";
                    var cat2 = it.TryGetProperty("cat_id2", out var c2) ? c2.ToString() : "";

                    root.Add(new XElement("Product",
                        new XElement("Id", id),
                        new XElement("Title", title),
                        new XElement("Currency", "CNY"),
                        new XElement("Price", priceStr),
                        new XElement("ShopName", sellerTitle),
                        new XElement("Url", url),
                        new XElement("Pic", pic),
                        new XElement("Sales", sales),
                        new XElement("Reviews", reviews),
                        new XElement("CategoryId", cat1),
                        new XElement("CategoryId2", cat2)
                    ));
                }
            }

            return new XDocument(root);
        }

        private static string FixUrl(string? u) =>
            string.IsNullOrWhiteSpace(u) ? "" :
            (u.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? u : $"https:{u}");

        private static string XPathLiteral(string s)
        {
            if (!s.Contains('\'')) return $"'{s}'";
            if (!s.Contains('"')) return $"\"{s}\"";
            var parts = s.Split('\'');
            return "concat(" + string.Join(", \"'\", ", parts.Select(p => $"'{p}'")) + ")";
        }
    }
}
