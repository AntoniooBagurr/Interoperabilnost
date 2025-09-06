using System.Xml.Serialization;

namespace InterOp.Server.Dto
{
    [XmlRoot("Product")]
    public class ProductXml
    {
        [XmlElement(Order = 1)] public string Id { get; set; } = "";
        [XmlElement(Order = 2)] public string Title { get; set; } = "";
        [XmlElement(Order = 3)] public string? Currency { get; set; }
        [XmlElement(Order = 4)] public decimal? Price { get; set; }
        [XmlElement(Order = 5)] public string? ShopName { get; set; }
        [XmlElement(Order = 6)] public string? Url { get; set; }
        [XmlElement(Order = 7)] public string? Pic { get; set; }
        [XmlElement(Order = 8)] public string? Sales { get; set; }
        [XmlElement(Order = 9)] public string? Reviews { get; set; }
        [XmlElement(Order = 10)] public string? CategoryId { get; set; }
        [XmlElement(Order = 11)] public string? CategoryId2 { get; set; }
    }
}
