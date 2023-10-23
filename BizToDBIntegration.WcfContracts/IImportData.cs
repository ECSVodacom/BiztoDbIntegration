using System.ServiceModel;
using System.Xml.Linq;

namespace BizToDBIntegration.WcfContracts
{
    [ServiceContract]
    public interface IImportData
    {
        [OperationContract]
        Response ImportXml(XmlDataType type, string uniqueIdentifier, XElement data);
        [OperationContract]
        Response ImportXmlString(XmlDataType type, string uniqueIdentifier, string data);
    }
}
