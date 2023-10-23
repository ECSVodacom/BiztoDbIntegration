using BizToDBIntegration.WcfContracts;
using Spar.Manager.RemittanceAdvice;
using Spar.Manager.StoreList;
using Spar.Manager.VendorList;
using Spar.Manager.Order;
using Spar.Manager.Invoice;
using System;
using System.ServiceModel;
using System.Xml.Linq;
using Spar.Manager.ReconImport;
using Spar.Manager.CreditNoteI;
using Spar.Manager.Claim;

namespace Spar
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class SparDataImportService : IImportData
    {
        private Response returnResponse = new Response();

        [FaultContract(typeof(SparDataImportService))]
        public Response ImportXml(XmlDataType type, string uniqueIdentifier, XElement xmlData)
        {
            var data = xmlData.ToString().Replace("xmlns=\"http://tempuri.org/\"", "");

            switch (type)
            {
                case XmlDataType.Order:
                    returnResponse.ResponseResult = ResponseResult.Failure;
                    returnResponse.ResponseMessage = new NotImplementedException().ToString();
                    break;
                case XmlDataType.Invoice:
                    returnResponse.ResponseResult = ResponseResult.Failure;
                    returnResponse.ResponseMessage = new NotImplementedException().ToString();
                    break;
                case XmlDataType.CreditNote:
                    returnResponse.ResponseResult = ResponseResult.Failure;
                    returnResponse.ResponseMessage = new NotImplementedException().ToString();
                    break;
                case XmlDataType.RemittanceAdvice:
                    RemittanceAdvice remittanceAdvice = new RemittanceAdvice();
                    returnResponse = remittanceAdvice.ProcessRemittanceAdvice(type, data);
                    break;
                case XmlDataType.StoreList:
                    StoreList storeList = new StoreList();
                    returnResponse = storeList.ProcessStoreList(type, data);
                    break;
                case XmlDataType.VendorList:
                    VendorList vendorList = new VendorList();
                    returnResponse = vendorList.Process(type, data);
                    break;
                case XmlDataType.ReconImport:
                    ReconImport reconImport = new ReconImport();
                    returnResponse = reconImport.Process(type, uniqueIdentifier, data);
                    break;
                case XmlDataType.DropShipOrder:
                    Order order = new Order();
                    returnResponse = order.Process(type, uniqueIdentifier, data);
                    break;
                case XmlDataType.DropShipInvoice:
                    Invoice invoice = new Invoice();
                    returnResponse = invoice.Process(type, uniqueIdentifier, data);
                    break;
                case XmlDataType.CreditNoteI:
                    var creditNoteI = new CreditNoteI();
                    returnResponse = creditNoteI.Process(type, uniqueIdentifier, data);
                    break;
                case XmlDataType.InvoiceI:
                    var invoiceI = new InvoiceI();
                    returnResponse = invoiceI.Process(type, uniqueIdentifier, data);

                    break;
                case XmlDataType.Claim:
                    var claim = new Claim();
                    returnResponse = claim.Process(type, uniqueIdentifier, data);

                    break;
            }
            returnResponse.UniqueIdentifier = uniqueIdentifier;
            returnResponse.Type = type;
            if (returnResponse.ResponseResult == ResponseResult.Exception)
            {
                string faultReason = string.Empty;
                foreach (var trace in returnResponse.Trace)
                {
                    faultReason += trace + "; ";
                }
                FaultCode fc = new FaultCode("Exception");
                throw new FaultException(faultReason, fc);
            }
            return returnResponse;
        }

        public Response ImportXmlString(XmlDataType type, string uniqueIdentifier, string data)
        {

            data = data.Replace("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\" ?>", string.Empty);
            data = data.Replace("<?xml version=\"1.0\" encoding=\"ISO_8859-1\" standalone=\"yes\" ?>", string.Empty);

            switch (type)
            {
                case XmlDataType.CreditNoteI:
                    var creditNoteI = new CreditNoteI();
                    returnResponse = creditNoteI.Process(type, uniqueIdentifier, data.Trim());
                    break;
                case XmlDataType.InvoiceI:
                    var invoiceI = new InvoiceI();
                    returnResponse = invoiceI.Process(type, uniqueIdentifier, data);
                    break;
                case XmlDataType.Claim:
                    var claim = new Claim();
                    returnResponse = claim.Process(type, uniqueIdentifier, data);

                    break;
            }


            returnResponse.UniqueIdentifier = uniqueIdentifier;
            returnResponse.Type = type;
            if (returnResponse.ResponseResult == ResponseResult.Exception)
            {
                string faultReason = string.Empty;
                foreach (var trace in returnResponse.Trace)
                {
                    faultReason += trace + "; ";
                }
                FaultCode fc = new FaultCode("Exception");
                throw new FaultException(faultReason, fc);
            }
            return returnResponse;
        }


    }
}
