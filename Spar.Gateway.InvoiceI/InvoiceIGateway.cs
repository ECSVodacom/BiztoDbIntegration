using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels.Invoices.InvoiceI;

namespace Spar.Gateway.InvoiceI
{
    public class InvoiceIGateway
    {
        public Response Save(string data, string uniqueIdentifier, Response response)
        {
            var linqXml = InvoiceDocument.Parse(data);

            return response;
        }

    }
}
}
