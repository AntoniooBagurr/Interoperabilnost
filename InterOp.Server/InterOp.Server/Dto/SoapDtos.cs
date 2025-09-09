using System.Runtime.Serialization;

namespace InterOp.Server.Dto
{
    [DataContract(Namespace = "http://interop")]
    public sealed class SoapProduct
    {
        [DataMember(Order = 1)] public string Id { get; set; } = "";
        [DataMember(Order = 2)] public string Title { get; set; } = "";
        [DataMember(Order = 3)] public string Currency { get; set; } = "CNY";
        [DataMember(Order = 4)] public decimal Price { get; set; }
        [DataMember(Order = 5)] public string ShopName { get; set; } = "";
        [DataMember(Order = 6)] public string Url { get; set; } = "";
        [DataMember(Order = 7)] public string Pic { get; set; } = "";
        [DataMember(Order = 8)] public string Sales { get; set; } = "";
        [DataMember(Order = 9)] public string Reviews { get; set; } = "";
        [DataMember(Order = 10)] public string CategoryId { get; set; } = "";
        [DataMember(Order = 11)] public string CategoryId2 { get; set; } = "";
    }

    [DataContract(Namespace = "http://interop")]
    public sealed class SoapSearchResponse
    {
        [DataMember(Order = 1)] public int Count { get; set; }
        [DataMember(Order = 2)] public SoapProduct[] Items { get; set; } = Array.Empty<SoapProduct>();
        // za demonstraciju zadatka – XML koji je backend generirao iz REST-a
        [DataMember(Order = 3)] public string RawXml { get; set; } = "";
    }

    public sealed class SoapOptions
    {
        public string SellerId { get; set; } = "14349340";
        public int Pages { get; set; } = 1;
        public int PageSize { get; set; } = 60;
    }
}
