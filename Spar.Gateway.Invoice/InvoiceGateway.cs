using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels.Invoices.DropShip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Xml.Linq;

namespace Spar.Gateway.Invoice
{
    internal sealed class InvoiceGateway
    {
        public Response CustomException(Response response, string message)
        {
            response.ResponseResult = ResponseResult.Failure;
            response.ResponseMessage = "Invoice(s) failed to save";
            response.Trace.Add(message);

            return response;
        }

        public Response Save(string xml, string UniqueIdentifier, Response response)
        {
            XDocument xDocument = XDocument.Parse(xml);
            List<string> storeEans = new List<string>();

            foreach (var element in xDocument.Descendants("UNB").Descendants("UNH").Descendants("CLO").Elements("CDPT"))
            {
                if (!storeEans.Contains(element.Value))
                {
                    storeEans.Add(element.Value);
                }
            }


            var linqXml = UNB.Parse(xml);
            var supplierEan = linqXml.UNH.FirstOrDefault().SAP.SAPT;
            var dcEan = linqXml.UNH.FirstOrDefault().CLO.COPT;
            var invoiceNumber = linqXml.UNH.FirstOrDefault().IRE.INVR.REFN;
            var storeEan = linqXml.UNH.FirstOrDefault().CLO.CDPT.ToString();
            var storeName = linqXml.UNH.FirstOrDefault().CLO.CDPN.ToString();
            //var storeAddress = linqXml.UNH.FirstOrDefault().CLO.CDPA.ToString();
            var dcId = 0;
            var supplierId = 0;
            IQueryable<Store> stores;


            using (var ts = new TransactionScope(TransactionScopeOption.Required,
                 new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                try
                {


                    using (SparDSHInvoicesDataContext db = new SparDSHInvoicesDataContext())
                    {
                        db.CommandTimeout = 600;

                        supplierId = (from sp in db.Suppliers
                                      where sp.SPcEANNumber == supplierEan
                                      select sp.SPID).FirstOrDefault();
                        if (supplierId == 0)
                        {
                            response.Trace.Add(string.Format("Invoice {0} supplier does not exist", invoiceNumber));
                            return CustomException(response, "Supplier does not exist");
                        }

                        dcId = (from d in db.DCs
                                where d.DCcEANNumber == dcEan
                                select d.DCID).FirstOrDefault();
                        if (dcId == 0)
                        {
                            response.Trace.Add(string.Format("Invoice {0} dc does not exist", invoiceNumber));
                            return CustomException(response, "DC does not exist");
                        }

                        stores = from s in db.Stores
                                 where s.STiDCID == dcId && storeEans.Contains(s.STcEANNumber)
                                 select s;


                        foreach (var unh in linqXml.UNH)
                        {
                            var receiveDate = Convert.ToDateTime(unh.translatedate == String.Empty ? DateTime.Now.ToString() : unh.translatedate.Replace("?", ""));
                            var translateDate = Convert.ToDateTime(unh.translatedate == String.Empty ? DateTime.Now.ToString() : unh.translatedate.Replace("?", ""));
                            var confirmDate = Convert.ToDateTime(unh.confirmdate == String.Empty ? DateTime.Now.ToString() : unh.confirmdate.Replace("?", ""));


                            var orderNumber = unh.ODD.ORNO.ORNU1;
                            invoiceNumber = unh.IRE.INVR.REFN;
                            var supplierDispatchpoint = unh.SDP.SUDP;
                            var postDate = DateTime.Now;
                            var invoiceId = 0;

                            #region DoValidationChecks
                            var s = stores.Where(store => store.STcEANNumber == storeEan).FirstOrDefault();

                            if (s == null)
                            {
                                var messageException = new MessageException
                                {
                                    MEcMessageNr = invoiceNumber,
                                    MEcType = "INVOICE",
                                    MEcDCEAN = dcEan,
                                    MEcStoreEAN = storeEan,
                                    MEcStoreName = storeName,
                                    MEcStoreMail = "",
                                    //MEcAddress = storeAddress,
                                    MEcSupplierEAN = supplierEan,
                                    MEcContact = null,
                                    MEcTelNo = null,
                                    MEcFaxNo = null,
                                    MEdReceivedDate = DateTime.Now,
                                    MEcException = "Store does not exist",
                                    MEcFileRef = "",
                                    MEiAction = 0
                                };

                                db.MessageExceptions.InsertOnSubmit(messageException);
                                db.SubmitChanges();
                                ts.Complete();

                                response.Trace.Add(string.Format("Invoice {0} store does not exist", invoiceNumber));
                                continue;
                            }

                            IEnumerable<Invoice> invoiceList = (from i in db.Invoices
                                                                where i.INcInvoiceNumber.Contains(invoiceNumber.TrimStart('0'))
                                                                && i.INcDCEAN == dcEan
                                                                && i.INcStoreEAN == storeEan
                                                                && i.INcSupplierEAN == supplierEan
                                                                select i).ToArray();



                            foreach (Invoice inv in invoiceList)
                                if (inv.INcInvoiceNumber.TrimStart('0') == invoiceNumber.TrimStart('0'))
                                {
                                    invoiceId = inv.INID;

                                    break;
                                }

                            if (invoiceId != 0)
                            {
                                response.Trace.Add(string.Format("Invoice {0} already exist", invoiceNumber));
                                continue;
                            }







                            DateTime? acknowledgedByDc = null;
                            if (orderNumber != "00")
                            {
                                InvoiceAcknowledgement invoiceAcknowledgement = (from ia in db.InvoiceAcknowledgements
                                                                                 where ia.InvoiceNumber.Contains(invoiceNumber.TrimStart('0'))
                                                                                                 && ia.DcEan == dcEan
                                                                                                 && ia.StoreEan == storeEan
                                                                                                 && ia.SupplierEan == supplierEan
                                                                                                 && ia.IsProcessed == false
                                                                                 select ia).FirstOrDefault();


                                if (invoiceAcknowledgement != null)
                                {
                                    acknowledgedByDc = invoiceAcknowledgement.AcknowledgedByDc;
                                    invoiceAcknowledgement.IsProcessed = true;
                                    invoiceAcknowledgement.ProcessedDateTime = DateTime.Now;
                                }


                                #endregion

                                var newOrderNumber = String.Format("{0}.{1}", orderNumber + "", s.STcCode);
                                IEnumerable<Order> ordersList = (from o in db.Orders
                                                                 where o.TRcTraceNumber.Contains(newOrderNumber)
                                                                     && o.TRiStoreID == s.STID
                                                                     && o.TRiSupplierID == supplierId
                                                                     && o.TRiDCID == dcId
                                                                 select o).ToArray();

                                Order order = null;
                                orderNumber = string.Empty;

                                foreach (Order o in ordersList)
                                    if (o.TRcTraceNumber.TrimStart('0') == newOrderNumber.TrimStart('0'))
                                    {
                                        order = o;

                                        break;
                                    }

                                int? orderId = null;
                                if (order != null)
                                {
                                    orderNumber = newOrderNumber;
                                    order.TRdConfirmDate = receiveDate;
                                    orderId = order.TRID;
                                }
                            }


                            var cradAdj1 = "0";
                            var cradPerc1 = "0";
                            var cradVal1 = "0";
                            var cradAdj2 = "0";
                            var cradPerc2 = "0";
                            var cradVal2 = "0";
                            var cradAdj3 = "0";
                            var cradPerc3 = "0";
                            var cradVal3 = "0";

                            var transportCstInc = "0";
                            var transportCstPerc = "0";
                            var transportCstVal = "0";

                            var dradAdj2 = "0";
                            var dutLevPerc = "0";
                            var dutLevVal = "0";

                            var lineSubTotalExclusive = "0";
                            var lineSubTotalVat = "0";

                            if (unh.VRS.Count > 0)
                            {
                                cradAdj1 = unh.VRS[0].CRAD.ADJI == String.Empty ? "0" : unh.VRS[0].CRAD.ADJI;
                                cradPerc1 = unh.VRS[0].CRAD.PERC == String.Empty ? "0" : unh.VRS[0].CRAD.PERC;
                                cradVal1 = unh.VRS[0].CRAD.VALU == String.Empty ? "0" : unh.VRS[0].CRAD.VALU;
                                cradAdj2 = unh.VRS[0].CRAD.ADJI2 == String.Empty ? "0" : unh.VRS[0].CRAD.ADJI2;
                                cradPerc2 = unh.VRS[0].CRAD.PERC2 == String.Empty ? "0" : unh.VRS[0].CRAD.PERC2;
                                cradVal2 = unh.VRS[0].CRAD.VALU2 == String.Empty ? "0" : unh.VRS[0].CRAD.VALU2;
                                cradAdj3 = unh.VRS[0].CRAD.ADJI3 == String.Empty ? "0" : unh.VRS[0].CRAD.ADJI3;
                                cradPerc3 = unh.VRS[0].CRAD.PERC3 == String.Empty ? "0" : unh.VRS[0].CRAD.PERC3;
                                cradVal3 = unh.VRS[0].CRAD.VALU3 == String.Empty ? "0" : unh.VRS[0].CRAD.VALU3;

                                transportCstInc = unh.VRS[0].DRAD.ADJI == String.Empty ? "0" : unh.VRS[0].DRAD.ADJI;
                                transportCstPerc = unh.VRS[0].DRAD.PERC == String.Empty ? "0" : unh.VRS[0].DRAD.PERC;
                                transportCstVal = unh.VRS[0].DRAD.VALU == String.Empty ? "0" : unh.VRS[0].DRAD.VALU;

                                dradAdj2 = unh.VRS[0].DRAD.ADJI2 == String.Empty ? "0" : unh.VRS[0].DRAD.ADJI2;
                                dutLevPerc = unh.VRS[0].DRAD.PERC2 == String.Empty ? "0" : unh.VRS[0].DRAD.PERC2;
                                dutLevVal = unh.VRS[0].DRAD.VALU2 == String.Empty ? "0" : unh.VRS[0].DRAD.VALU2;

                                lineSubTotalExclusive = unh.VRS[0].LSTA == String.Empty ? "0" : unh.VRS[0].LSTA;
                                lineSubTotalVat = unh.VRS[0].VATA == String.Empty ? "0" : unh.VRS[0].VATA;
                            }



                            var settleDiscountPercentage = "0";
                            var settleDiscountValue = "0";
                            if (unh.SDI != null)
                            {
                                settleDiscountPercentage = unh.SDI.SETT.PERC == String.Empty ? "0" : unh.SDI.SETT.PERC;
                                settleDiscountValue = unh.SDI.SETT.VALU == String.Empty ? "0" : unh.SDI.SETT.VALU;
                            }
                            var extSubTotalExclusive = unh.IPD.LNTA == String.Empty ? "0" : unh.IPD.LNTA;
                            var totalVat = unh.IPD.TVAT == String.Empty ? "0" : unh.IPD.TVAT;
                            var extSubTotalInclusive = unh.IPD.TPAY == String.Empty ? "0" : unh.IPD.TPAY;
                            var deliveryDate = unh.ODD.DELR.DATE == String.Empty ? "" : unh.ODD.DELR.DATE;
                            var invoiceDate = unh.IRE.INVR.DATE1 == String.Empty ? "" : unh.IRE.INVR.DATE1;
                            var authNo = unh.ODD.CDNO.CNDN == String.Empty ? "0" : unh.ODD.CDNO.CNDN;

                            var year = String.Empty;
                            var month = String.Empty;
                            var day = String.Empty;

                            if (invoiceDate.Length == 6)
                            {
                                year = invoiceDate.Substring(0, 2);
                                month = invoiceDate.Substring(2, 2);
                                day = invoiceDate.Substring(4, 2);
                                invoiceDate = String.Format("20{0}/{1}/{2}", year, month, day);
                            }

                            if (deliveryDate.Length == 6)
                            {
                                year = deliveryDate.Substring(0, 2);
                                month = deliveryDate.Substring(2, 2);
                                day = deliveryDate.Substring(4, 2);
                                deliveryDate = String.Format("20{0}/{1}/{2}", year, month, day);
                            }


                            var invoice = new Invoice
                            {
                                INcInvoiceNumber = invoiceNumber,
                                INdInvoiceDate = invoiceDate,
                                INcOrderID = orderNumber,
                                INcDCEAN = dcEan,
                                INcSupplierEAN = supplierEan,
                                INcStoreEAN = storeEan,
                                INdReceivedDate = receiveDate,
                                INdTranslateDate = translateDate,
                                INdPostDate = postDate,
                                INcCDAdjIndicator1 = cradAdj1,
                                INiCDPerc1 = cradPerc1,
                                INmCDValue1 = cradVal1,
                                INcCDAdjIndicator2 = cradAdj2,
                                INiCDPerc2 = cradPerc2,
                                INmCDValue2 = cradVal2,
                                INcCDAddDisInd = cradAdj3,
                                INiCDAddDiscPerc = cradPerc3,
                                INmCDAddDiscValue = cradVal3,
                                INcTransportCstInc = transportCstInc,
                                INiTransportCstPerc = transportCstPerc,
                                INmTransportCstVal = transportCstVal,
                                INcDutLevIndc = dradAdj2,
                                INiDutLevPerc = dutLevPerc,
                                INmDutLevVal = dutLevVal,
                                INmLnSubTotExl = lineSubTotalExclusive,
                                INmLnSubTotVat = lineSubTotalVat,
                                INmExtSubTotExl = extSubTotalExclusive,
                                INmTotVat = totalVat,
                                INmExtSubTotIncl = extSubTotalInclusive,
                                INiSettleDisVal = settleDiscountValue,
                                INiSettleDisPerc = settleDiscountPercentage,
                                INcDelivDate = deliveryDate,
                                INcAuthNo = authNo,
                                INdRecDcBackDate = acknowledgedByDc,
                                INcDespatchPoint = supplierDispatchpoint,

                            };

                            db.Invoices.InsertOnSubmit(invoice);
                            db.SubmitChanges();

                            foreach (var item in unh.ILD)
                            {
                                var freeQty = "0";
                                if (item.FRDL.SUPC == null)
                                {
                                    var tempNewFreeQty = item.FRDL.ToString().Replace("<FRDL>", String.Empty).Replace("</FRDL>", String.Empty);
                                    freeQty = tempNewFreeQty == String.Empty ? "0" : tempNewFreeQty;
                                }

                                var qty = item.QDEL.NODU == String.Empty ? "0" : item.QDEL.NODU;
                                var listCost = item.COST.COSP == String.Empty ? "0" : item.COST.COSP;
                                var adjustmentOne = item.CRAD.ADJI == String.Empty ? "0" : item.CRAD.ADJI;
                                var adjustmentPercentageOne = item.CRAD.PERC == String.Empty ? "0" : item.CRAD.PERC;
                                var adjustmentValueOne = item.CRAD.VALU == String.Empty ? "0" : item.CRAD.VALU;
                                var adjustmentTwo = item.CRAD.ADJI2 == String.Empty ? "0" : item.CRAD.ADJI2;
                                var adjustmentPercentageTwo = item.CRAD.PERC2 == String.Empty ? "0" : item.CRAD.PERC2;
                                var adjustmentValueTwo = item.CRAD.VALU2 == String.Empty ? "0" : item.CRAD.VALU2;
                                var nelC = item.NELC == String.Empty ? "0" : item.NELC;
                                var vatP = item.VATP == String.Empty ? "0" : item.VATP;
                                var vatC = item.VATC == String.Empty ? "0" : item.VATC;
                                var consumerBarCode = item.PROC.EANC;
                                var consumerOrderUnit = item.PROC.EANC2;
                                var supplierProductCode = item.PROC.SUPC;
                                var productDescription = item.PROC.PROD;
                                var supplierPack = item.QDEL.CUDU == String.Empty ? "0" : item.QDEL.CUDU;
                                var unitOfMeasure = item.QDEL.UNOM == String.Empty ? "0" : item.QDEL.UNOM;

                                var invoiceDetail = new InvoiceDetail
                                {
                                    IDiInvoiceNumber = invoice.INID,
                                    IDcConsumerBarCode = consumerBarCode,
                                    IDcConsumerOrdUnit = consumerOrderUnit,
                                    IDcSupplProdCode = supplierProductCode,
                                    IDcProdDescription = productDescription,
                                    IDcUnitOfMeasure = unitOfMeasure,
                                    IDiSupplierPack = supplierPack,
                                    IDiFreeQty = freeQty,
                                    IDiQty = qty,
                                    IDmListCost = listCost,
                                    IDcAdjIndicator1 = adjustmentOne,
                                    IDmAdjValue1 = adjustmentValueOne,
                                    IDiAdjPerc1 = adjustmentPercentageOne,
                                    IDcAdjIndicator2 = adjustmentTwo,
                                    IDmAdjValue2 = adjustmentValueTwo,
                                    IDiAdjPerc2 = adjustmentPercentageTwo,
                                    IDmNettValue = nelC,
                                    IDiVatPerc = vatP,
                                    IDcVatCode = vatC
                                };
                                db.InvoiceDetails.InsertOnSubmit(invoiceDetail);
                            }


                            IEnumerable<Claim> claimList = (from c in db.Claims
                                                            where c.CLcInvoiceNumber.Contains(invoiceNumber.TrimStart('0'))
                                                                && c.CLiDCID == dcId
                                                                && c.CLiStoreID == s.STID
                                                                && c.CLiSupplierID == supplierId
                                                            select c).ToArray();
                            Claim claim = null;
                            foreach (Claim clm in claimList)
                                if (clm.CLcInvoiceNumber.TrimStart('0') == invoiceNumber.TrimStart('0'))
                                {
                                    claim = clm;

                                    break;
                                }

                            if (claim != null)
                            {
                                claim.CLiInvoiceID = invoice.INID;
                            }

                            db.SubmitChanges();

                            response.Trace.Add(string.Format("Invoice {0} added", invoiceNumber));

                        }
                    }
                    ts.Complete();
                    response.ResponseResult = ResponseResult.Success;
                    response.ResponseMessage = "Invoice(s) processed";

                }
                catch (Exception e)
                {
                    response.ResponseResult = ResponseResult.Exception;
                    response.ResponseMessage = "Invoice(s) failed writing to DB";
                    response.Trace.Add(e.Message);
                    return response;
                }
            }
            return response;
        }

    }
}
