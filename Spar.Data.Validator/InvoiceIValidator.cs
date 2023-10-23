﻿using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Spar.Data.Validators
{
    public class InvoiceIValidator
    {
        private readonly Response returnResponse = new Response();


        public Response ValidateInvoiceI(string xmlData)
        {
            XDocument xml = XDocument.Parse(xmlData);
            XmlSchemaSet schemas = new XmlSchemaSet();
            var success = true;

            returnResponse.ResponseResult = ResponseResult.Success;
            returnResponse.Trace.Add("Validating");

            schemas.Add("", Schema.GetInvoiceISchema());


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
