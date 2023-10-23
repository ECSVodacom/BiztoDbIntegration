using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using BizToDBIntegration.WcfContracts;
using Shoprite.Data.XsdModels;

namespace Shoprite.Gateway
{
    internal class InvoiceGateway
    {
        public Response Save(string xml, Response returnResponse)
        {
            using (var ts = new TransactionScope())
            {
                try
                {
                    // TODO: Save to database
                    var linqToXml = EnterpriseDocument.Parse(xml);
                    var a = (from c in linqToXml.Name
                             select c);


                    ts.Complete();

                    returnResponse.ResponseResult = ResponseResult.Success;
                    returnResponse.ResponseMessage = "Transaction saved to DB";
                    returnResponse.Trace.Add("Saving order(s) to DB Successful");

                    return returnResponse;
                }
                catch (Exception ex)
                {
                    returnResponse.ResponseResult = ResponseResult.Exception;
                    returnResponse.ResponseMessage = "Transaction failed writing to DB";
                    returnResponse.Trace.Add(ex.Message);

                    return returnResponse;
                }
            }
        }
    }
}