using BizToDBIntegration.WcfContracts;
using Spar.Data.Validators;
using Spar.Gateway.CreditNoteI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spar.Manager.CreditNoteI
{
    public class CreditNoteI
    {
        private static Response response = new Response();

        public Response Process(XmlDataType type, string UniqueIdentifier, string data)
        {
           /* try
            {*/
                var creditNoteIValidator = new CreditNoteIValidator();
                response = creditNoteIValidator.ValidateCreditNoteI(data);

                if (response.ResponseResult == ResponseResult.Success)
                {
                    CreditNoteIGateway gateway = new CreditNoteIGateway();
                    response = gateway.Save(data, UniqueIdentifier, response);
                }
                else
                {
                    response.ResponseResult = ResponseResult.Exception;
                    response.Trace.Add("Validation of XML on the XSD failed");
                }

                return response;
           /*}
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.Trace.Add(ex.Message);
                return response;
            }*/
        }
    }
}
