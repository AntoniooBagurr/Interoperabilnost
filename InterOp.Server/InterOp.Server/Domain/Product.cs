namespace InterOp.Server.Domain;

public class Product
{
    public int Id { get; set; }            
    public string ExtId { get; set; } = ""; 
    public string Title { get; set; } = "";
    public string? Currency { get; set; }
    public decimal? Price { get; set; }
    public string? ShopName { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
