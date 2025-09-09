using InterOp.Server.Dto;
using System.ServiceModel;

namespace InterOp.Server.Services.Soap
{
    [ServiceContract(Namespace = "http://interop")]
    public interface IProductSoapService
    {
        [OperationContract]
        SoapSearchResponse Search(string term, string? sellerId, int pages, int pageSize);
    }
}
