using System.ServiceModel;
using System.Xml.Linq;
using BizToDBIntegration.WcfContracts;
using TradingGateway.Manager;

namespace TradingGateway
{
    public class TradingGatewayDataImportService : IImportData
    {
        private Response returnResponse = new Response();

        [FaultContract(typeof(TradingGatewayDataImportService))]
        public Response ImportXml(XmlDataType type, string uniqueIdentifier, XElement xmlData)
        {
            switch (type)
            {
                case XmlDataType.Order:
                    {
                        var order = new Order();
                        returnResponse = order.Process(type, xmlData.ToString());
                        break;
                    }
            }
            returnResponse.UniqueIdentifier = uniqueIdentifier;
            returnResponse.Type = type;
            if (returnResponse.ResponseResult == ResponseResult.Failure)
            {
                var faultReason = new FaultReason(returnResponse.ResponseMessage);
                var fc = new FaultCode("Failure");
                throw new FaultException(faultReason, fc);
            }
            return returnResponse;
        }

        public Response ImportXmlString(XmlDataType type, string uniqueIdentifier, string data)
        {
            throw new System.NotImplementedException();
        }
    }
}