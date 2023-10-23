using BizToDBIntegration.WcfContracts;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;

namespace Shoprite.Gateway.ProductList
{
    public class ProductListGateway
    {
        public Response ProductImport(XElement data, Response returnResponse, string division)
        {

            try
            {
                using (ShopriteProductImportModelDataContext db = new ShopriteProductImportModelDataContext())
                {
                    var shopritedivision = db.Divisions.Where(dv => dv.Name == division).FirstOrDefault();
                    if (shopritedivision != null)
                    {
                        var products = db.Products.Where(pr => pr.Division_Id == shopritedivision.Id);
                        db.DeleteShopriteProducts(shopritedivision.Id);
                        //using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Required,
                        //        new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
                        //{
                        //    foreach (var prod in products)
                        //    {
                        //        db.Products.DeleteOnSubmit(prod);
                        //        db.SubmitChanges();
                        //    }

                        //    scope.Complete();
                        //}


                        var listOfProducts = data.Descendants("SupplierItem");
                        foreach (var item in listOfProducts)
                        {
                            using (TransactionScope ts = new TransactionScope())
                            {
                                string val = (item.Attribute("ItemDescription") != null) ? item.Attribute("ItemDescription").Value : "";
                                string pcode = (item.Attribute("ProductCode") != null) ? item.Attribute("ProductCode").Value : "";
                                string barcode = (item.Attribute("PreferredBarcode") != null) ? item.Attribute("PreferredBarcode").Value : "";
                                string divisor = (item.Attribute("Divisor") != null) ? item.Attribute("Divisor").Value : "";

                                var product = db.Products.Where(p => p.Ean == barcode.Trim() && p.SupplierCode == pcode.Trim() && p.Division_Id == shopritedivision.Id).FirstOrDefault();
                                if (product == null)
                                {
                                    product = new Product()
                                    {
                                        Ean = barcode.Trim(),
                                        SupplierCode = pcode.Trim(),
                                        Description = val.Trim(),
                                        PackUnits = 0,
                                        CostPrice = 0,
                                        Division = shopritedivision,
                                        Divisor = (divisor != "") ? Convert.ToDecimal(divisor) : Convert.ToDecimal(null),

                                    };

                                    db.Products.InsertOnSubmit(product);
                                    try
                                    {
                                        db.SubmitChanges(ConflictMode.ContinueOnConflict);
                                    }
                                    catch (ChangeConflictException e)
                                    {
                                        foreach (ObjectChangeConflict occ in db.ChangeConflicts)
                                        {
                                            //No database values are merged into current.
                                            occ.Resolve(RefreshMode.OverwriteCurrentValues);
                                        }

                                    }

                                }
                                ts.Complete();
                            }
                        }

                    }

                }

                returnResponse.ResponseResult = ResponseResult.Success;
                returnResponse.ResponseMessage = "Transaction saved to DB";
                returnResponse.Trace.Add("Saving order(s) to DB Successful");

                return returnResponse;
            }
            catch (Exception ex)
            {
                throw new Exception("Transaction failed writing to DB " + ex.InnerException.ToString());
            }
                
             

        }
    }
}
