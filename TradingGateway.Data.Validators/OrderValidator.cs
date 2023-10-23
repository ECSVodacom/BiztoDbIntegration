using System;
using System.Xml.Linq;
using System.Xml.Schema;
using BizToDBIntegration.WcfContracts;
using TradingGateway.Data.XsdModels;

namespace TradingGateway.Data.Validators
{
    internal sealed class OrderValidator
    {
        private readonly Response _response = new Response();

        public Response Validate(string xmlData)
        {
            bool success = true;

            try
            {
                XDocument xml = XDocument.Parse(xmlData);
                var schemas = new XmlSchemaSet();


                _response.ResponseResult = ResponseResult.Success;
                _response.Trace.Add("Validating order on schema");

                schemas.Add("", Schema.GetOrderSchema());


                xml.Validate(schemas, (o, e) =>
                    {
                        _response.ResponseResult = ResponseResult.Failure;
                        _response.ResponseMessage = e.Message;
                        _response.Trace.Add("Validation of order on schema failed");
                        success = false;
                    });
            }
            catch (Exception e)
            {
                _response.ResponseResult = ResponseResult.Failure;
                _response.ResponseMessage = e.Message;
                _response.Trace.Add("Validation of order on schema failed");
                success = false;
            }

            if (success)
            {
                _response.Trace.Add("Validation of order on schema successful");
            }

            return _response;
        }
    }
}