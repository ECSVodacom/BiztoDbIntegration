using BizToDBIntegration.WcfContracts;
using DocumentTracking.Data.Validators;
using DocumentTracking.Gateway;
using System;

namespace DocumentTracking.Manager
{
    internal sealed class Document
    {
        private static Response response = new Response();

        public Response Process(XmlDataType type, string data)
        {
            try
            {
                var documentValidator = new DocumentValidator();
                response = documentValidator.Validate(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    // Write to DB
                    var documentGateway = new DocumentGateway();
                    response = documentGateway.Save(data, response);

                }
                else
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Validation on XML failed";
                    response.Trace.Add("Validation on XML against XSD failed");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }

        internal Response ProcessCreditNote(XmlDataType type, string data)
        {
            try
            {
                var documentValidator = new DocumentValidator();
                response = documentValidator.ValidateCreditNote(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    // Write to DB
                    var documentGateway = new DocumentGateway();
                    response = documentGateway.SaveCreditNote(data, response);

                }
                else
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Validation on XML failed";
                    response.Trace.Add("Validation on XML against XSD failed");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }

        public Response ProcessOrder(XmlDataType type, string data)
        {
            try
            {
                var documentValidator = new DocumentValidator();
                response = documentValidator.ValidateOrder(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    // Write to DB
                    var documentGateway = new DocumentGateway();
                    response = documentGateway.SaveOrder(data, response);

                }
                else
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Validation on XML failed";
                    response.Trace.Add("Validation on XML against XSD failed");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }

        internal Response ProcessGRN(XmlDataType type, string data)
        {
            try
            {
                var documentValidator = new DocumentValidator();
                response = documentValidator.ValidateGRN(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    // Write to DB
                    var documentGateway = new DocumentGateway();
                    response = documentGateway.SaveGRN(data, response);

                }
                else
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Validation on XML failed";
                    response.Trace.Add("Validation on XML against XSD failed");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }

        internal Response ProcessClaim(XmlDataType type, string data)
        {
            try
            {
                var documentValidator = new DocumentValidator();
                response = documentValidator.ValidateClaim(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    // Write to DB
                    var documentGateway = new DocumentGateway();
                    response = documentGateway.SaveClaim(data, response);

                }
                else
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Validation on XML failed";
                    response.Trace.Add("Validation on XML against XSD failed");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }

        public Response ProcessInvoice(XmlDataType type, string data)
        {
            try
            {
                var documentValidator = new DocumentValidator();
                response = documentValidator.ValidateOrder(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    // Write to DB
                    var documentGateway = new DocumentGateway();
                    response = documentGateway.SaveInvoice(data, response);

                }
                else
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Validation on XML failed";
                    response.Trace.Add("Validation on XML against XSD failed");
                }

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }
    }
}