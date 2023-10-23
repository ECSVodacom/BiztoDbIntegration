using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using BizToDBIntegration.WcfContracts;
using Shoprite.Data.XsdModels;
using System.Xml.Linq;

namespace Shoprite.Gateway.InvoiceList
{
    internal class InvoiceGateway
    {
        public Response Process(XElement data, Response returnResponse)
        {
            using (var ts = new TransactionScope())
            {
                try
                {
                    var invoiceData = data.Descendants("data").Single().Value;
                    var header = data.Descendants("header").Single();
                    XElement idata = XElement.Parse(invoiceData);
                    var listOfInvoices = idata.Descendants("SupplierItem");

                    using (ShopriteInvoiceModelDataContext db = new ShopriteInvoiceModelDataContext())
                    {
                        foreach (var item in listOfInvoices)
                        {
                            string barcode = (item.Attribute("supplierItemID") != null) ? item.Attribute("supplierItemID").Value : "";
                            var product = db.Products.Where(pr => pr.Ean == item.Attribute("supplierItemID").Value);
                            if(product !=null)
                            {
                                item.SetAttributeValue("supplierItemID",product.Select(pr => pr.SupplierCode).SingleOrDefault());
                            }
                                
                        }
                    }

                    XElement complete = new XElement("message", header, new XElement("data", idata));
                    
           

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
