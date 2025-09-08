using System.Text.Json.Serialization;

namespace InterOp.Server.Services;

public sealed class TaobaoRoot { public TaobaoResult result { get; set; } = new(); }
public sealed class TaobaoResult
{
    public TaobaoStatus status { get; set; } = new();
    public List<TaobaoItem> item { get; set; } = new();
    public string seller_title { get; set; } = "";
}
public sealed class TaobaoStatus
{
    public int code { get; set; }
    public string msg { get; set; } = "";
}

public sealed class TaobaoItem
{
    public long num_iid { get; set; }
    public string title { get; set; } = "";
    public decimal? price { get; set; }
    public string detail_url { get; set; } = "";
    public string? pic { get; set; }
    public string? sales { get; set; }
    public string? reviews { get; set; }
    public long? cat_id { get; set; }
    public long? cat_id2 { get; set; }
}

