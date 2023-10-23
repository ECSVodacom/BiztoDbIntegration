using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.Invoice;
using System;


namespace Spar.Manager.Invoice
{
    public class InvoiceI
    {
        private static Response response = new Response();

        public Response Process(XmlDataType type, string UniqueIdentifier, string data)
        {
            try
            {
                InvoiceIValidator validator = new InvoiceIValidator();


                var invoiceIValidator = new InvoiceIValidator();
                response = invoiceIValidator.ValidateInvoiceI(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    InvoiceIGateway gateway = new InvoiceIGateway();
                    response = gateway.Save(data, UniqueIdentifier, response);
                }
                else
                {
                    response.ResponseResult = ResponseResult.Exception;
                    response.Trace.Add("Validation of XML on the XSD failed");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.Trace.Add(ex.Message);
                return response;
            }
        }
    }
}
