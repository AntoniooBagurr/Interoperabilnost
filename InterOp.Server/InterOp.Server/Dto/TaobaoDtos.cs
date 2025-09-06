using System.Text.Json.Serialization;

namespace InterOp.Server.Services;

public sealed class TaobaoRoot { public TaobaoResult result { get; set; } = new(); }
public sealed class TaobaoResult
{
    public string seller_title { get; set; } = "";
    public List<TaobaoItem> item { get; set; } = new();
}
public sealed class TaobaoItem
{
    public long num_iid { get; set; }
    public string title { get; set; } = "";
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal? price { get; set; }
    public string detail_url { get; set; } = "";
}
