using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spar.Manager.Order
{
    internal sealed class Order
    {
        private static Response response = new Response();

        public Response Process(XmlDataType type, string UniqueIdentifier, string data)
        {
            try
            {
                var orderValidator = new OrderValidator();
                response = orderValidator.ValidateOrderDSH(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    OrderGateway gateway = new OrderGateway();
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
