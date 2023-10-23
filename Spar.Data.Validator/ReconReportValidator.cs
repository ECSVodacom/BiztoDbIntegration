using System.Xml.Linq;
using System.Xml.Schema;
using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels;
using System;

namespace Spar.Data.Validators
{
    internal sealed class ReconReportValidator
    {
        private readonly Response response = new Response();

        public Response Validate(string xmlData)
        {
            bool success = true;

            try
            {
                XDocument xml = XDocument.Parse(xmlData);
                var schemas = new XmlSchemaSet();

                response.ResponseResult = ResponseResult.Success;
                response.Trace.Add("Validating document on schema");

                schemas.Add("", Schema.GetReconImportSchema());

                xml.Validate(schemas, (o, e) =>
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = e.Message;
                    response.Trace.Add("Validation of the XML on the schema failed");
                    success = false;
                });
            }
            catch (Exception e)
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation of the XML on the schema failed");
                success = false;
            }

            if (success)
            {
                response.Trace.Add("Validation of XML on schema successful");
            }

            return response;
        }
    }
}
