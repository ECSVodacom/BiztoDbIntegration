using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.VendorList;


namespace Spar.Manager.VendorList
{
    class VendorList
    {
        private static Response returnResponse = new Response();

        public Response Process(XmlDataType type, string data)
        {
            data = data
                .Replace("xmlns=\"http://spar.co.za/gdim/mdm/vlv1:SPARVendors\"", "");
        
            var vendorListValidator = new VendorListValidator();

            returnResponse = vendorListValidator.Validate(data);

            if (returnResponse.ResponseResult == ResponseResult.Success)
            {
                // Write to DB
                var vendorListGateway = new VendorListGateway();

                returnResponse = vendorListGateway.Save(data, returnResponse);
            }
            else
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = "Validation on vendor list XML failed";
                returnResponse.Trace.Add("Validation on vendor list XML against XSD failed");
            }

            return returnResponse;
        }
    }
}
