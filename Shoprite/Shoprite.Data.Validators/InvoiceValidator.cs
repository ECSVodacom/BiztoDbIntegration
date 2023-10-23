using System;
using System.Xml.Linq;
using System.Xml.Schema;
using BizToDBIntegration.WcfContracts;
using Shoprite.Data.XsdModels;

namespace Shoprite.Data.Validators
{
    internal class InvoiceValidator
    {
        private readonly Response _response = new Response();

        public Response Validate(string xmlData)
        {
            bool success = true;

            try
            {
                XDocument xml = XDocument.Parse(xmlData);
                var schemas = new XmlSchemaSet();
                schemas.Add("", Schema.GetInvoiceSchema());

                _response.ResponseResult = ResponseResult.Success;
                _response.Trace.Add("Validating invoice on schema");


                xml.Validate(schemas, (o, e) =>
                    {
                        _response.ResponseResult = ResponseResult.Failure;
                        _response.ResponseMessage = e.Message;
                        _response.Trace.Add("Validation of invoice on schema failed");
                        success = false;
                    });
            }
            catch (Exception e)
            {
                _response.ResponseResult = ResponseResult.Failure;
                _response.ResponseMessage = e.Message;
                _response.Trace.Add("Validation of invoice on schema failed");
                success = false;
            }

            if (success)
            {
                _response.Trace.Add("Validation of invoice on schema successful");
            }

            return _response;
        }
    }
}