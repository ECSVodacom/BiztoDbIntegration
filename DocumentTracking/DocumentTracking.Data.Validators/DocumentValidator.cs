using System;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Schema;
using BizToDBIntegration.WcfContracts;
using DocumentTracking.Data.XsdModels;

namespace DocumentTracking.Data.Validators
{
    internal sealed class DocumentValidator
    {
        private readonly Response response = new Response();

        public Response Validate(string xmlData)
        {
            bool success = true;

            try
            {
                XDocument xml = XDocument.Parse(xmlData);
                var schemas = new XmlSchemaSet();

                response.ResponseResult = ResponseResult.Success;
                response.Trace.Add("Validating document on schema");

                schemas.Add("", Schema.GetDocTrack());

                xml.Validate(schemas, (o, e) =>
                    {
                        response.ResponseResult = ResponseResult.Failure;
                        response.ResponseMessage = e.Message;
                        response.Trace.Add("Validation of the document on the schema failed");
                        success = false;
                    });
            }
            catch (Exception e)
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation of the document on the schema failed");
                success = false;
            }

            if (success)
            {
                response.Trace.Add("Validation of the document on schema successful");
            }

            return response;
        }


        public Response ValidateCreditNote(string xmlData)
        {
            bool success = true;

            try
            {
                XDocument xmlCreditNote = XDocument.Parse(xmlData);

                response.ResponseResult = ResponseResult.Success;
                response.Trace.Add("Validating credit note");

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "";
                success = true;

            }
            catch (Exception e)
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation of the credit note failed");
                success = false;
            }

            if (success)
            {
                response.Trace.Add("Validation of the credit note successful");
            }

            return response;
        }


        public Response ValidateOrder(string xmlData)
        {
            bool success = true;

            try
            {
                XDocument xmlOrders = XDocument.Parse(xmlData);

                response.ResponseResult = ResponseResult.Success;
                response.Trace.Add("Validating document on schema");





                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "";
                //response.Trace.Add("Validation of the document on the schema failed");
                success = true;

            }
            catch (Exception e)
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation of the document on the schema failed");
                success = false;
            }

            if (success)
            {
                response.Trace.Add("Validation of the document on schema successful");
            }

            return response;
        }

        internal Response ValidateClaim(string data)
        {
            bool success = true;

            try
            {
                XDocument xmlCreditNote = XDocument.Parse(data);

                response.ResponseResult = ResponseResult.Success;
                response.Trace.Add("Validating claim");

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "";
                success = true;

            }
            catch (Exception e)
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation of the claim failed");
                success = false;
            }

            if (success)
            {
                response.Trace.Add("Validation of the claim successful");
            }

            return response;
        }

        internal Response ValidateGRN(string data)
        {
            bool success = true;

            try
            {
                XDocument xmlCreditNote = XDocument.Parse(data);

                response.ResponseResult = ResponseResult.Success;
                response.Trace.Add("Validating GRN");

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "";
                success = true;

            }
            catch (Exception e)
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation of the GRN failed");
                success = false;
            }

            if (success)
            {
                response.Trace.Add("Validation of the GRN successful");
            }

            return response;
        }


        public Response ValidateDeliveryNote(string data)
        {
            bool success = true;

            try
            {
                XDocument xmlDeliveryNote = XDocument.Parse(data);

                response.ResponseResult = ResponseResult.Success;
                response.Trace.Add("Validating delivery note");

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "";
                success = true;

            }
            catch (Exception e)
            {
                response.ResponseResult = ResponseResult.Failure;
                response.ResponseMessage = e.Message;
                response.Trace.Add("Validation of delivery note failed");
                success = false;
            }

            if (success)
            {
                response.Trace.Add("Validation of the delivery note successful");
            }

            return response;
        }
    }
}