using System.ServiceModel;
using System.Xml.Linq;
using BizToDBIntegration.WcfContracts;
using DocumentTracking.Manager;

namespace DocumentTracking
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class DocumentTrackingDataImportService : IImportData
    {
        private Response response = new Response();

        [FaultContract(typeof(DocumentTrackingDataImportService))]
        public Response ImportXml(XmlDataType type, string uniqueIdentifier, XElement xmlData)
        {
            switch (type)
            {
                case XmlDataType.DocumentTracking:
                    {
                        var document = new Document();
                        response = document.Process(type, xmlData.ToString());
                        break;
                    }
                case XmlDataType.Order:
                    {
                        var document = new Document();
                        response = document.ProcessOrder(type, xmlData.ToString());
                        break;
                    }


            }
            response.UniqueIdentifier = uniqueIdentifier;
            response.Type = type;
            //if (response.ResponseResult == ResponseResult.Failure)
            //{
            //    var faultReason = new FaultReason(response.ResponseMessage);
            //    var fc = new FaultCode("Failure");
            //    throw new FaultException(faultReason, fc);
            //}
            if (response.ResponseResult == ResponseResult.Exception)
            {
                string faultReason = string.Empty;
                foreach (var trace in response.Trace)
                {
                    faultReason += trace + "; ";
                }
                FaultCode fc = new FaultCode("Exception");
                throw new FaultException(faultReason, fc);
            }
            return response;
        }

        [FaultContract(typeof(DocumentTrackingDataImportService))]
        public Response ImportXmlString(XmlDataType type, string uniqueIdentifier, string data)
        {
            switch (type)
            {
                case XmlDataType.Order:
                    {
                        var document = new Document();
                        response = document.ProcessOrder(type, data);
                        break;
                    }
                case XmlDataType.Invoice:
                    {
                        var document = new Document();
                        response = document.ProcessInvoice(type, data);
                        break;
                    }
                case XmlDataType.CreditNote:
                    {
                        var document = new Document();
                        response = document.ProcessCreditNote(type, data);
                        break;
                    }
                case XmlDataType.Claim:
                    {
                        var document = new Document();
                        response = document.ProcessClaim(type, data);
                        break;
                    }
                case XmlDataType.GRN:
                    {
                        var document = new Document();
                        response = document.ProcessGRN(type, data);
                        break;
                    }
                case XmlDataType.PTHPRP:
                    {
                        var pthprp = new PTHPRP();
                        response = pthprp.Process(data);
                        break;
                    }
                case XmlDataType.DeliveryNote:
                    {
                        var note = new DeliveryNote();
                        response = note.Process(data);
                        break;
                    }
            }
            response.UniqueIdentifier = uniqueIdentifier;
            response.Type = type;
            //if (response.ResponseResult == ResponseResult.Failure)
            //{
            //    var faultReason = new FaultReason(response.ResponseMessage);
            //    var fc = new FaultCode("Failure");
            //    throw new FaultException(faultReason, fc);
            //}
            if (response.ResponseResult == ResponseResult.Exception)
            {
                string faultReason = string.Empty;
                foreach (var trace in response.Trace)
                {
                    faultReason += trace + "; ";
                }
                FaultCode fc = new FaultCode("Exception");
                throw new FaultException(faultReason, fc);
            }
            return response;
        }
    }
}