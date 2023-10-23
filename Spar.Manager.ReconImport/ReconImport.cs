using BizToDBIntegration.WcfContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spar.Data.Validators;
using Spar.Gateway.ReconImport;


namespace Spar.Manager.ReconImport
{
    internal sealed class ReconImport
    {
        private static Response response = new Response();

        public Response Process(XmlDataType type,string UniqueIdentifier, string data)
        {
            try
            {
                var reconValidator = new ReconReportValidator();
                response = reconValidator.Validate(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    // Write to DB
                    ReconImportGateway gateway = new ReconImportGateway();
                    response = gateway.Save(data, UniqueIdentifier, response);

                }
                else
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Validation on XML failed";
                    response.Trace.Add("Validation on XML against XSD failed");
                }

                return response;
            }
            catch(Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }
    }
}
