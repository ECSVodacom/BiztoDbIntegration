using System.Xml.Linq;
using System.Xml.Schema;
using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels;

namespace Spar.Data.Validators
{
    class VendorListValidator
    {
        private readonly Response returnResponse = new Response();

        public Response Validate(string xmlData)
        {
            returnResponse.Trace.Add("Validating vendor list against schema");

            var xml = XDocument.Parse(xmlData);
            var schemas = new XmlSchemaSet();

            schemas.Add("", Schema.GetVendorListSchema());

            var success = true;

            xml.Validate(schemas, (o, e) =>
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = e.Message;
                returnResponse.Trace.Add("Vendor list validation failed");
                success = false;

            });

            if (success)
            {
                returnResponse.ResponseResult = ResponseResult.Success;
                returnResponse.Trace.Add("Vendor list validated");
            }

            return returnResponse;
        }
    }
}
