using BizToDBIntegration.WcfContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Xml.Linq;

namespace OverTheCounterPayment
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple)]
    internal class OverTheCounterPaymentService : IImportData
    {
        private Response returnResponse = new Response();

        [FaultContract(typeof(OverTheCounterPaymentService))]
        public Response ImportXml(XmlDataType type, string uniqueIdentifier, XElement xmlData)
        {
            var data = xmlData.ToString().Replace("xmlns=\"http://tempuri.org/\"", "");

            switch (type)
            {
                case XmlDataType.NRT:
                    returnResponse.ResponseResult = ResponseResult.Success;
                    returnResponse.ResponseMessage = "<YouSaid>" + data + "</YouSaid>";

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
