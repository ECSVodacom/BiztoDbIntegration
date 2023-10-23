using BizToDBIntegration.WcfContracts;
using DocumentTracking.Data.Validators;
using DocumentTracking.Gateway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentTracking.Manager
{
    internal sealed class DeliveryNote
    {
        private static Response _response = new Response();


        public DeliveryNote()
        {

        }

        internal Response Process(string data)
        {
            var validator = new DocumentValidator();

            _response = validator.ValidateDeliveryNote(data);

            if (_response.ResponseResult == ResponseResult.Success)
            {
                // Write to DB
                var gateway = new DocumentGateway();

                _response = gateway.SaveDeliveryNote(data, _response);
            }
            else
            {
                _response.ResponseResult = ResponseResult.Failure;
                _response.ResponseMessage = "Validation on XML failed";
                _response.Trace.Add("Validation on XML against XSD failed");
            }

            return _response;
        }
    }
}
