using BizToDBIntegration.WcfContracts;
using DocumentTracking.Data.Validators;
using System;

namespace DocumentTracking.Manager
{
    internal class PTHPRP
    {
        private static Response _response = new Response();

        public PTHPRP()
        {
        }

        internal Response Process(string data)
        {
            var validator = new PTHPRPValidator();

            _response = validator.Validate(data);

            if (_response.ResponseResult == ResponseResult.Success)
            {
                // Write to DB
                var pthprpGateway = new PTHPRPGateway();

                _response = pthprpGateway.Save(data, _response);
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