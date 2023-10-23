using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.Claim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spar.Manager.Claim
{
    public class Claim
    {
        private static Response response = new Response();

        public Response Process(XmlDataType type, string uiqueIdentifier, string data)
        {
            try
            {
                var claimValidator = new ClaimValidator();
                response = claimValidator.Validate(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    ClaimGateway gateway = new ClaimGateway();
                    response = gateway.Save(data, uiqueIdentifier, response);
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