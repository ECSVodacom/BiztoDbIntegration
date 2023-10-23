using BizToDBIntegration.WcfContracts;
using System;
using System.Transactions;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;

namespace Spar.Gateway.Order
{
    internal sealed class OrderGateway
    {
        public enum OrderStatus : int
        {
            FileReceivedByGateway = 1,
            TranslationToEdiOrXml,
            DeliveredToMailbox,
            ExtractedBySupplier,
            InvoiceGeneratedBySupplier,
            OrderConfirmed
        }

        public Response CustomException(Response response, string message)
        {
            response.ResponseResult = ResponseResult.Exception;
            response.ResponseMessage = "Order failed to save";
            response.Trace.Add(message);

            return response;
        }

        public Response Save(string xml, string UniqueIdentifier, Response response)
        {
            using (var ts = new TransactionScope(TransactionScopeOption.Required,
                   new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                try
                {
                    xml = xml.Replace("&", "&amp;");

                    var linqXml = Spar.Data.XsdModels.Orders.DropShip.UNB.Parse(xml);

                    var year = linqXml.UNH.DIN.LDAT.ToString().Substring(0, 2);
                    var month = linqXml.UNH.DIN.LDAT.ToString().Substring(2, 2);
                    var day = linqXml.UNH.DIN.LDAT.ToString().Substring(4, 2);

                    var newDate = Convert.ToDateTime(String.Format("20{0}/{1}/{2}", year, month, day));

                    string orderNumber = linqXml.UNH.ORD.ORNO.ORNU[0];
                    string dcEan = linqXml.UNH.CLO.ALIP.ToString();
                    string supplierEan = linqXml.UNH.SOP.SOPT.ToString();
                    string storeEan = linqXml.UNH.CLO.CDPT.ToString();
                    var deliveryDate = newDate;
                    var transactionCode = "N";
                    var receiveDate = Convert.ToDateTime(linqXml.UNH.recievedate.ToString() == String.Empty ? DateTime.Now.ToString() : linqXml.UNH.recievedate.ToString());
                    var setDiscount = 0;
                    var trade1Ind = linqXml.UNH.PRA[0].CRAD.ADJI1;
                    var trade1Val = Convert.ToDecimal(linqXml.UNH.PRA[1].CRAD.PERC1.ToString() == String.Empty ? "0" : linqXml.UNH.PRA[1].CRAD.PERC1.ToString(), CultureInfo.InvariantCulture);
                    var trade2Ind = linqXml.UNH.PRA[1].CRAD.ADJI1;
                    var trade2Val = Convert.ToDecimal(linqXml.UNH.PRA[1].CRAD.PERC1.ToString() == String.Empty ? "0" : linqXml.UNH.PRA[1].CRAD.PERC1.ToString(), CultureInfo.InvariantCulture);
                    var translatedDate = Convert.ToDateTime(linqXml.UNH.translatedate.ToString() == String.Empty ? DateTime.Now.ToString() : linqXml.UNH.translatedate.ToString(), CultureInfo.InvariantCulture);
                    var mailBoxDate = translatedDate;

                    using (SparDSOrdersDataContext db = new SparDSOrdersDataContext())
                    {
                        var supplierOrderMethod = (from lookup in db.SupplierDCLookups
                                                   where lookup.BuEanCode == dcEan
                                                    && lookup.LocationCode == supplierEan
                                                   select lookup.SupplierOrderMethod).FirstOrDefault();

                        if (supplierOrderMethod != null && supplierOrderMethod.Contains("E-mail"))
                        {
                            if (translatedDate == null)
                                translatedDate = DateTime.Now;

                            if (mailBoxDate == null)
                                mailBoxDate = DateTime.Now;
                        }

                        var dcId = (from d in db.DCs
                                    where d.DCcEANNumber == dcEan
                                    select d.DCID).FirstOrDefault();

                        if (dcId == 0)
                            return CustomException(response, "DC does not exist");

                        var supplierId = (from sp in db.Suppliers
                                          where sp.SPcEANNumber == supplierEan
                                          select sp.SPID).FirstOrDefault();

                        if (supplierId == 0)
                            return CustomException(response, "Supplier does not exist");

                        var s = (from st in db.Stores
                                 where st.STcEANNumber == storeEan
                                    && st.STiDCID == dcId
                                 select new
                                 {
                                     Id = st.STID,
                                     Code = st.STcCode
                                 }).FirstOrDefault();

                        if (s == null)
                            return CustomException(response, "Store does not exist");

                        var newOrderNumber = String.Format("{0}.{1}", orderNumber, s.Code);
                        var orderCount = (from o in db.Orders
                                          where o.TRcTraceNumber == newOrderNumber
                                            && o.TRiDCID == dcId
                                            && o.TRiSupplierID == supplierId
                                            && o.TRiStoreID == s.Id
                                          select o).Count();

                        if (orderCount > 0)
                            return CustomException(response, "Order already exists");

                        var now = DateTime.Now;

                        Order order = new Order
                        {
                            TRcTraceNumber = newOrderNumber,
                            TRiDCID = dcId,
                            TRcDCEAN = dcEan,
                            TRiSupplierID = supplierId,
                            TRcSupplierEAN = supplierEan,
                            TRiStoreID = s.Id,
                            TRcStoreEAN = storeEan,
                            TRdDeliveryDate = deliveryDate,
                            TRcTransactionCode = transactionCode,
                            TRdEDITime = now,
                            TRdReceivedDate = receiveDate,
                            TRdMailBoxTime = mailBoxDate,
                            TRdExtractDate = null,
                            TRcTrade1Indc = trade1Ind,
                            TRmTrade1 = trade1Val,
                            TRcTrade2Indc = trade2Ind,
                            TRmTrade2 = trade2Val,
                            TRmDiscount = setDiscount
                        };
                        db.Orders.InsertOnSubmit(order);
                        db.SubmitChanges();

                        var lineNumber = 0;

                        foreach (var item in linqXml.UNH.OLD)
                        {
                            lineNumber += 1;
                            var consumberBarCode = item.PROC.EANC.ToString();
                            var orderBarCode = item.PROC.EANC2.ToString();
                            var supplierProductCode = item.PROC.SUPC.ToString();
                            var productDescription = item.PROC.PROD.ToString();
                            var quantity = Convert.ToInt32(item.QNTO.NROU.ToString());
                            var confirmQuantity = 0;
                            var unitMeasure = item.QNTO.UNOM.ToString();
                            var supplierPack = Convert.ToInt32(item.QNTO.CONU.ToString() == String.Empty ? "0" : item.QNTO.CONU.ToString());
                            var listCost = Convert.ToDecimal(item.COST.COSP.ToString() == String.Empty ? "0" : item.COST.COSP.ToString(), CultureInfo.InvariantCulture);
                            var confirmListCost = 0;
                            var deal1 = Convert.ToDecimal(item.CRAD.PERC1.ToString() == String.Empty ? "0" : item.CRAD.PERC1.ToString(), CultureInfo.InvariantCulture);
                            var confirmDeal1 = "0";
                            var deal2 = Convert.ToDecimal(item.CRAD.PERC2.ToString() == String.Empty ? "0" : item.CRAD.PERC2.ToString(), CultureInfo.InvariantCulture);
                            var confirmDeal2 = "0";
                            var netCost = Convert.ToDecimal(item.NELC.ToString() == String.Empty ? "0" : item.NELC.ToString(), CultureInfo.InvariantCulture);
                            var confirmNetCost = 0;
                            var vat = Convert.ToInt32(item.VATP.ToString() == String.Empty ? "0" : item.VATP.ToString());
                            var confirmVat = 0;
                            var freeQty = Convert.ToInt32(item.FREE.NROU.ToString() == String.Empty ? "0" : item.FREE.NROU.ToString());
                            var confirmFreeQuantity = 0;

                            OrderDetail orderDetail = new OrderDetail
                            {
                                TDiTrackID = order.TRID,
                                TDcTraceNumber = orderNumber,
                                TDiLineNumber = lineNumber,
                                TDcConsumerBarCode = consumberBarCode,
                                TDcOrderBarCode = orderBarCode,
                                TDcSuppProdCode = supplierProductCode,
                                TDcProdDescr = productDescription,
                                TDiQuantity = quantity,
                                TDiConfirmQuantity = confirmQuantity,
                                TDcUnitMeasure = unitMeasure,
                                TDiSupplierPack = supplierPack,
                                TDmListCost = listCost,
                                TDmConfirmListCost = confirmListCost,
                                TDmDeal1 = deal1,
                                TDcDeal1indic = confirmDeal1,
                                TDmDeal2 = deal2,
                                TDcDeal2indic = confirmDeal2,
                                TDmNetCost = netCost,
                                TDmConfirmNetCost = confirmNetCost,
                                TDiVat = vat,
                                TDiConfirmVat = confirmVat,
                                TRiFreeQty = freeQty,
                                TRiConfirmFreeQty = confirmFreeQuantity
                            };
                            db.OrderDetails.InsertOnSubmit(orderDetail);
                        }
                        db.SubmitChanges();


                        db.AddToOrdersAuditLog(order.TRID, (int)OrderStatus.FileReceivedByGateway, null, DateTime.Now);

                        if (translatedDate != null)
                            db.AddToOrdersAuditLog(order.TRID, (int)OrderStatus.TranslationToEdiOrXml, null, DateTime.Now);

                        if (mailBoxDate != null)
                            db.AddToOrdersAuditLog(order.TRID, (int)OrderStatus.DeliveredToMailbox, null, DateTime.Now);

                    }

                    ts.Complete();
                    response.ResponseResult = ResponseResult.Success;
                    response.ResponseMessage = "Order saved to DB";
                    response.Trace.Add("Order added");
                }
                catch (Exception e)
                {
                    response.ResponseResult = ResponseResult.Exception;
                    response.ResponseMessage = "Order failed writing to DB";
                    response.Trace.Add(e.Message);
                    return response;
                }
            }

            return response;
        }
    }
}
