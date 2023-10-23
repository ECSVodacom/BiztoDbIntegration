using System.ServiceModel;
using System.Xml.Linq;
using BizToDBIntegration.WcfContracts;
using Shoprite.Manager;
using System.Configuration;

namespace Shoprite
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class ShopriteDataImportService : IImportData
    {
        private Response returnResponse = new Response();

        [FaultContract(typeof (ShopriteDataImportService))]
        public Response ImportXml(XmlDataType type, string uniqueIdentifier, XElement xmlData)
        {
            switch (type)
            {
                case XmlDataType.ProductList:
                    {
                        var productList = new ProductList();
                        returnResponse = productList.Process(type, xmlData, ConfigurationManager.AppSettings["ShopriteDivision"].ToString());

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