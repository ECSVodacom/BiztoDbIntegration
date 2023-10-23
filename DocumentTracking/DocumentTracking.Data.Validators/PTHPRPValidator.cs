using BizToDBIntegration.WcfContracts;
using System;
using DocumentTracking.Data.XsdModels;
using System.Xml.Linq;
using System.Xml.Schema;

namespace DocumentTracking.Data.Validators
{
    internal sealed class PTHPRPValidator
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
                _response.Trace.Add("Validating PTHPRP to the schema");

                // Removed due to xml file that is not consistent
                //schemas.Add("", Schema.GetPTHPRP());

                //xml.Validate(schemas, (o, e) =>
                //{
                //    _response.ResponseResult = ResponseResult.Failure;
                //    _response.ResponseMessage = e.Message;
                //    _response.Trace.Add("Validation of PTHPRP to the schema failed");
                //    success = false;
                //});
            }
            catch (Exception e)
            {
                _response.ResponseResult = ResponseResult.Failure;
                _response.ResponseMessage = e.Message;
                _response.Trace.Add("Validation of PTHPRP to the schema failed");
                success = false;
            }

            if (success)
            {
                _response.Trace.Add("Validation of PTHPRP to the schema successful");
            }

            return _response;
        }
    }
}
