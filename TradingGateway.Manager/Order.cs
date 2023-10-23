using BizToDBIntegration.WcfContracts;
using TradingGateway.Data.Validators;
using TradingGateway.Gateway;

namespace TradingGateway.Manager
{
    internal sealed class Order
    {
        private static Response returnResponse = new Response();

        public Response Process(XmlDataType type, string data)
        {
            data = data.Replace("ns0:", "").Replace("xmlns:ns0=\"http://TradeGateway.InsertOrder\"", "");
            var order = new OrderValidator();

            returnResponse = order.Validate(data);

            if (returnResponse.ResponseResult == ResponseResult.Success)
            {
                // Write to DB
                var orderGateway = new OrderGateway();

                returnResponse = orderGateway.Save(data, returnResponse);
            }
            else
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = "Validation on XML failed";
                returnResponse.Trace.Add("Validation on XML against XSD failed");
            }

            return returnResponse;
        }
    }
}