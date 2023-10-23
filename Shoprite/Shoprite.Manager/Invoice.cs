using BizToDBIntegration.WcfContracts;
using Shoprite.Data.Validators;
using Shoprite.Gateway.InvoiceList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shoprite.Manager
{
    internal class Invoice
    {
        private static Response returnResponse = new Response();

        public Response Process(XmlDataType type, XElement data)
        {

            try
            {
                var invoiceGateway = new InvoiceGateway();
                returnResponse = invoiceGateway.Process(data, returnResponse);
            }
            catch(Exception ex)
            {
            }
             
               
            //}
            //else
            //{
            //    returnResponse.ResponseResult = ResponseResult.Failure;
            //    returnResponse.ResponseMessage = "Validation on XML failed";
            //    returnResponse.Trace.Add("Validation on XML against XSD failed");
            //}

            return returnResponse;
        }
    }
}