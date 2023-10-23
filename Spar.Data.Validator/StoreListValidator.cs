using System.Xml.Linq;
using System.Xml.Schema;
using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels;

namespace Spar.Data.Validators
{
    internal sealed class StoreListValidator
    {
        private readonly Response returnResponse = new Response();

        public Response ValidateStoreList(string xmlData)
        {
            returnResponse.Trace.Add("Validating store list against schema");

            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();

            schemas.Add("", Schema.GetStoreListSchema());

            var success = true;

            xml.Validate(schemas, (o, e) =>
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = e.Message;
                returnResponse.Trace.Add("Store list validation failed");
                success = false;
            });

            if (success)
            {
                returnResponse.ResponseResult = ResponseResult.Success;
                returnResponse.Trace.Add("Store list validated");
            }

            return returnResponse;
        }
    }
}
