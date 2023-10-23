using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.StoreList;

namespace Spar.Manager.StoreList
{
    internal sealed class StoreList
    {
        private static Response returnResponse = new Response();

        public Response ProcessStoreList(XmlDataType type, string data)
        {
            StoreListValidator validate = new StoreListValidator();

            returnResponse = validate.ValidateStoreList(data);

            if (returnResponse.ResponseResult == ResponseResult.Success)
            {
                // Write to DB
                var storeListGateway = new StoreListGateway();

                returnResponse = storeListGateway.Save(data, returnResponse);
            }
            else
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = "Validation on store list XML failed";
                returnResponse.Trace.Add("Validation on store list XML against XSD failed");
            }

            return returnResponse;
        }

    }
}
