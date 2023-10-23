using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Spar.Data.Validators
{
    public class ClaimValidator
    {
        private readonly Response response = new Response();


        public Response Validate(string xmlData)
        {
            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();
            var success = true;

            response.ResponseResult = ResponseResult.Success;
            response.Trace.Add("Validating");

            schemas.Add("", Schema.GetClaimSchema());


            xml.Validate(schemas, (o, e) =>
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation failed");
                success = false;
            });

            if (success)
            {
                response.Trace.Add("Validated");
            }

            return response;
        }
    }
}
