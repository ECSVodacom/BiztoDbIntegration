using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.Invoice;
using System;


namespace Spar.Manager.Invoice
{
    internal sealed class Invoice
    {
        private static Response response = new Response();

        public Response Process(XmlDataType type, string UniqueIdentifier, string data)
        {
            try
            {
                var invoiceValidator = new  InvoiceValidator();
                response = invoiceValidator.ValidateInvoiceDSH(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    InvoiceGateway gateway = new InvoiceGateway();
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
