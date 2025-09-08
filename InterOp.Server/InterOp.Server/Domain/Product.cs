using Microsoft.EntityFrameworkCore;

namespace InterOp.Server.Domain
{
    public class Product
    {
        public int Id { get; set; }
        public string ExtId { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Currency { get; set; }
        [Precision(18, 2)]
        public decimal? Price { get; set; }
        public string? ShopName { get; set; }
        public string? Url { get; set; }
        public string? Pic { get; set; }
        public string? Sales { get; set; }
        public string? Reviews { get; set; }
        public string? CategoryId { get; set; }
        public string? CategoryId2 { get; set; }
        public string? RawXml { get; set; } = "";

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
