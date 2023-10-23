using BizToDBIntegration.WcfContracts;
using System.Xml.Linq;
using System.Xml.Schema;
using Spar.Data.XsdModels;

namespace Spar.Data.Validators
{
    public class CreditNoteIValidator
    {
        private readonly Response returnResponse = new Response();


        public Response ValidateCreditNoteI(string xmlData)
        {
            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();
            var success = true;

            returnResponse.ResponseResult = ResponseResult.Success;
            returnResponse.Trace.Add("Validating");

            schemas.Add("", Schema.GetCreditNoteISchema());


            xml.Validate(schemas, (o, e) =>
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = e.Message;
                returnResponse.Trace.Add("Validation failed");
                success = false;
            });

            if (success)
            {
                returnResponse.Trace.Add("Validated");
            }

            return returnResponse;
        }
    }
}
