using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.RemittanceAdvice;

namespace Spar.Manager.RemittanceAdvice
{
    internal sealed class RemittanceAdvice
    {
        private static Response returnResponse = new Response();

        public Response ProcessRemittanceAdvice(XmlDataType type, string data)
        {            
            RemittanceAdviceValidator validate = new Spar.Data.Validators.RemittanceAdviceValidator();
            if (data.ToLower().Contains("remittancedocumentv2"))
            {
                returnResponse = validate.ValidateRAXml(data);

                if (returnResponse.ResponseResult == ResponseResult.Success)
                {
                    //Write to DB
                    var raGateway = new RemittanceAdviceGateway();

                    returnResponse = raGateway.CreateRA(data, returnResponse);
                }
                else
                {
                    returnResponse.ResponseResult = ResponseResult.Failure;
                    returnResponse.ResponseMessage = "Validation on XML failed";
                    returnResponse.Trace.Add("Validation on XML against XSD failed");
                }
            }
            else
            {
                if (data.ToLower().Contains("<ratype>dc</ratype>"))
                {                    
                    if (data.ToLower().Contains("<totalallowancededucteddesc>"))
                    {
                        returnResponse = validate.ValidateRAXmlOldDC(data);
                        if (returnResponse.ResponseResult == ResponseResult.Success)
                        {
                            var raGateway = new RemittanceAdviceGateway();
                            returnResponse = raGateway.CreateOldRA(data, returnResponse);
                        }
                        else
                        {
                            returnResponse.ResponseResult = ResponseResult.Failure;
                            returnResponse.ResponseMessage = "Validation on XML failed";
                            returnResponse.Trace.Add("Validation on XML against XSD failed");
                        }
                    }
                    else
                    {
                        // data contains <totalcommissiondeducteddesc>
                        returnResponse = validate.ValidateRAXmlOldDCCommission(data);
                        if (returnResponse.ResponseResult == ResponseResult.Success)
                        {
                            var raGateway = new RemittanceAdviceGateway();
                            returnResponse = raGateway.CreateOldRA(data, returnResponse);
                        }
                        else
                        {
                            returnResponse.ResponseResult = ResponseResult.Failure;
                            returnResponse.ResponseMessage = "Validation on XML failed";
                            returnResponse.Trace.Add("Validation on XML against XSD failed");
                        }
                    }
                }
                else if (data.ToLower().Contains("<ratype>dsh</ratype>"))
                {
                    returnResponse = validate.ValidateRAXmlOldDS(data);
                    if (returnResponse.ResponseResult == ResponseResult.Success)
                    {
                        var raGateway = new RemittanceAdviceGateway();
                        returnResponse = raGateway.CreateOldRA(data, returnResponse);
                    }
                    else
                    {
                        returnResponse.ResponseResult = ResponseResult.Failure;
                        returnResponse.ResponseMessage = "Validation on XML failed";
                        returnResponse.Trace.Add("Validation on XML against XSD failed");
                    }
                }
            }
            return returnResponse;
        }
    }
}
