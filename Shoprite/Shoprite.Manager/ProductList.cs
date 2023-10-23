using BizToDBIntegration.WcfContracts;
using Shoprite.Gateway.ProductList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shoprite.Manager
{
    internal class ProductList
    {
        private static Response returnResponse = new Response();
        public Response Process(XmlDataType type, XElement data, string division)
        {
            try
            {
                var productData = data.Descendants("data").Single().Value;
                XElement pdata = XElement.Parse(productData);
                var RecordCount = pdata.Attribute("RecordCount").Value;
                var ProductCount = pdata.Descendants("SupplierItem").Count();
                if (RecordCount == ProductCount.ToString())
                {
                    var productGateway = new ProductListGateway();
                    returnResponse = productGateway.ProductImport(pdata, returnResponse, division);
                }
                else
                {
                    returnResponse.ResponseResult = ResponseResult.Failure;
                    returnResponse.ResponseMessage = "Record count not matching with line items count";
                }

                return returnResponse;

            }
            catch (Exception ex)
            {
                throw new Exception("Transaction failed writing to DB " + ex.InnerException.ToString());
                //returnResponse.ResponseResult = ResponseResult.Failure;
                //returnResponse.ResponseMessage = ex.Message;
            }
          
        }
    }
}
