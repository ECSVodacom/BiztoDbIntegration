using System.Xml.Linq;
using System.Xml.Schema;
using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels;

namespace Spar.Data.Validators
{
    internal sealed class RemittanceAdviceValidator
    {
        private readonly Response returnResponse = new Response();        

        public Response ValidateRAXml(string xmlData)
        {
            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();
            var success = true;
            
            returnResponse.ResponseResult = ResponseResult.Success;
            returnResponse.Trace.Add("Validating Remittance Advice");

            schemas.Add("", Schema.GetRemittanceAdviceSchema());

            
            xml.Validate(schemas, (o, e) => 
                {
                    returnResponse.ResponseResult = ResponseResult.Failure;
                    returnResponse.ResponseMessage = e.Message;
                    returnResponse.Trace.Add("Validating Remittance Advice failed");
                    success = false;
                });

            if (success)
            {
                returnResponse.Trace.Add("Validating Remittance Advice Successful");
            }
                        
            return returnResponse;
        }

        public Response ValidateRAXmlOldDC(string xmlData)
        {
            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();
            var success = true;

            returnResponse.ResponseResult = ResponseResult.Success;
            returnResponse.Trace.Add("Validating Old DC Remittance Advice");

            schemas.Add("", Schema.GetRemittanceAdviceSchemaOldDC());

            xml.Validate(schemas, (o, e) =>
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = e.Message;
                returnResponse.Trace.Add("Validating Old DC Remittance Advice failed");
                success = false;
            });

            if (success)
            {
                returnResponse.Trace.Add("Validating Old DC Remittance Advice Successful");
            }
            
            return returnResponse;
        }

        public Response ValidateRAXmlOldDCCommission(string xmlData)
        {
            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();
            var success = true;

            returnResponse.ResponseResult = ResponseResult.Success;
            returnResponse.Trace.Add("Validating Old DC Remittance Advice with Commission");

            schemas.Add("", Schema.GetRemittanceAdviceSchemaOldDCCommission());

            xml.Validate(schemas, (o, e) =>
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = e.Message;
                returnResponse.Trace.Add("Validating Old DC Remittance Advice with Commission failed");
                success = false;
            });

            if (success)
            {
                returnResponse.Trace.Add("Validating Old DC Remittance Advice with Commission Successful");
            }

            return returnResponse;
        }

        public Response ValidateRAXmlOldDS(string xmlData)
        {
            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();
            var success = true;

            returnResponse.ResponseResult = ResponseResult.Success;
            returnResponse.Trace.Add("Validating Old DSH Remittance Advice");

            schemas.Add("", Schema.GetRemittanceAdviceSchemaOldDS());

            xml.Validate(schemas, (o, e) =>
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = e.Message;
                returnResponse.Trace.Add("Validating Old DSH Remittance Advice failed");
                success = false;
            });

            if (success)
            {
                returnResponse.Trace.Add("Validating Old DSH Remittance Advice Successful");
            }

            return returnResponse;
        }
        
    }
}
