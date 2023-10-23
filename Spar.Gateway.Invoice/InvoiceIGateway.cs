using BizToDBIntegration.WcfContracts;
using System;
using System.Transactions;
using System.Linq;
using System.Collections.Generic;
using Spar.Data.XsdModels.Invoices.InvoiceI;

namespace Spar.Gateway.Invoice
{
    public class InvoiceIGateway
    {
        public Response CustomException(Response response, string message)
        {
            response.ResponseResult = ResponseResult.Failure;
            response.ResponseMessage = "Invoice failed to save";
            response.Trace.Add(message);

            return response;
        }
        public Response Save(string data, string uniqueIdentifier, Response response)
        {
            using (var ts = new TransactionScope(TransactionScopeOption.Required,
              new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                try
                {
                    var linqXml = InvoiceDocument.Parse(data);
                    foreach (var taxInvoice in linqXml.TAXINV)
                    {
                        using (SparDSHInvoicesDataContext db = new SparDSHInvoicesDataContext())
                        {
                            var unb = taxInvoice.UNB;
                            var unh = unb.UNH;

                            var orderNumber = unh.ODD.OrderNumberAndDate.CustomerOrderNumber.ToString();
                            var dateOrderPlacedByCustomer = unh.ODD.OrderNumberAndDate.DateOrderPlacedByCustomer;
                            var invoiceNumber = unh.IRE.InvoiceReferences.Number.ToString().TrimStart('0');
                            var invoiceDate = unh.IRE.InvoiceReferences.Date.ToString("yyyy")
                                + "/" + unh.IRE.InvoiceReferences.Date.ToString("MM")
                                + "/" + unh.IRE.InvoiceReferences.Date.ToString("dd");
                            var supplierEan = unh.SAP.SupplierAccountingPoint.ToString();
                            var supplierDispatchpoint = unh.SDP == null ? unb.INTERCHANGESENDER.SenderIdentification.ToString() : unh.SDP.SupplierDespatchPoint.ToString();
                            var postDate = DateTime.Now;
                            var invoiceId = 0;
                            var dcEan = unh.CLO.CustomerOrderPoint.ToString();
                            var storeEan = unh.CLO.CustomerDeliveryInvoicePoint.ToString();
                            var storeName = unh.CLO.CustomerDeliveryPointName.ToString();
                            var receivedDate = unb.CreationDateTime == null ? DateTime.Now : unb.CreationDateTime.Date;

                            IEnumerable<Invoice> invoiceList = (from i in db.Invoices
                                                                where i.INcInvoiceNumber.Contains(invoiceNumber)
                                                                && i.INcDCEAN == dcEan
                                                                && i.INcStoreEAN == storeEan
                                                                && i.INcSupplierEAN == supplierEan
                                                                select i).ToArray();

                            foreach (Invoice inv in invoiceList)
                                if (inv.INcInvoiceNumber.TrimStart('0') == invoiceNumber)
                                {
                                    invoiceId = inv.INID;

                                    break;
                                }

                            if (linqXml.TAXINV.Count > 1 && invoiceId != 0)
                            {
                                response.Trace.Add(string.Format("Invoice {0} already exist", invoiceNumber));
                                continue;
                            }
                            else if (invoiceId != 0)
                            {
                                return CustomException(response, "The selected Invoice already exist for the selected order");
                            }


                            InvoiceAcknowledgement invoiceAcknowledgement = (from ia in db.InvoiceAcknowledgements
                                                                             where ia.InvoiceNumber.Contains(invoiceNumber)
                                                                                             && ia.DcEan == dcEan
                                                                                             && ia.StoreEan == storeEan
                                                                                             && ia.SupplierEan == supplierEan
                                                                                             && ia.IsProcessed == false
                                                                             select ia).FirstOrDefault();

                            DateTime? acknowledgedByDc = null;
                            if (invoiceAcknowledgement != null)
                            {
                                acknowledgedByDc = invoiceAcknowledgement.AcknowledgedByDc;
                                invoiceAcknowledgement.IsProcessed = true;
                                invoiceAcknowledgement.ProcessedDateTime = DateTime.Now;
                            }



                            var supplierId = (from sp in db.Suppliers
                                              where sp.SPcEANNumber == supplierEan
                                              select sp.SPID).FirstOrDefault();

                            if (linqXml.TAXINV.Count > 1 && supplierId == 0)
                            {
                                response.Trace.Add(string.Format("Supplier for invoice {0} does not exist", invoiceNumber));
                                continue;
                            }
                            else if (supplierId == 0)
                            {
                                return CustomException(response, "Supplier does not exist");
                            }

                            var dcId = (from d in db.DCs
                                        where d.DCcEANNumber == dcEan
                                        select d.DCID).FirstOrDefault();

                            if (linqXml.TAXINV.Count > 1 && dcId == 0)
                            {
                                response.Trace.Add(string.Format("DC for invoice {0} does not exist", invoiceNumber));
                                continue;
                            }
                            else if (dcId == 0)
                            {
                                return CustomException(response, "DC does not exist");
                            }

                            var s = (from st in db.Stores
                                     where st.STcEANNumber == storeEan
                                        && st.STiDCID == dcId
                                     select new
                                     {
                                         Id = st.STID,
                                         Code = st.STcCode
                                     }).FirstOrDefault();

                            if (linqXml.TAXINV.Count > 1 && s == null)
                            {
                                response.Trace.Add(string.Format("Store for invoice {0} does not exist", invoiceNumber));
                                continue;
                            }
                            if (s == null)
                            {
                                var messageException = new MessageException
                                {
                                    MEcMessageNr = "",
                                    MEcType = "INVOICE",
                                    MEcDCEAN = dcEan,
                                    MEcStoreEAN = storeEan,
                                    MEcStoreName = storeName,
                                    MEcStoreMail = "",
                                    MEcAddress = null,
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

                                return CustomException(response, "Store does not exist");
                            }

                            var newOrderNumber = String.Format("{0}.{1}", orderNumber, s.Code);
                            IEnumerable<Order> ordersList = (from o in db.Orders
                                                             where o.TRcTraceNumber.Contains(newOrderNumber)
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
                                order.TRdConfirmDate = receivedDate;
                                orderId = order.TRID;
                            }

                            var invoice = new Invoice
                            {
                                INcInvoiceNumber = invoiceNumber,
                                INcOrderID = orderNumber,
                                INcDCEAN = dcEan,
                                INcSupplierEAN = supplierEan,
                                INcStoreEAN = storeEan,
                                INdRecDcBackDate = acknowledgedByDc,
                                INcDespatchPoint = supplierDispatchpoint,
                                INdInvoiceDate = invoiceDate,
                                INdReceivedDate = receivedDate,
                                INdTranslateDate = receivedDate,
                                INdPostDate = postDate,
                                INmLnSubTotExl = unb.IPD.LinesNetTotalCostExclVat.ToString(),
                                INmLnSubTotVat = unb.IPD.TotalVatAmountPayable.ToString(),
                                INmExtSubTotExl = unb.IPD.LinesNetTotalCostExclVat.ToString(),
                                INmTotVat = unb.IPD.TotalVatAmountPayable.ToString(),
                                INmExtSubTotIncl = unb.IPD.TotalAmountPayableInclVat.ToString(),
                                INcCDAdjIndicator1 = "0",
                                INiCDPerc1 = "0",
                                INmCDValue1 = "0",
                                INcCDAdjIndicator2 = "0",
                                INiCDPerc2 = "0",
                                INmCDValue2 = "0",
                                INcCDAddDisInd = "0",
                                INiCDAddDiscPerc = "0",
                                INmCDAddDiscValue = "0",
                                INcTransportCstInc = "0",
                                INiTransportCstPerc = "0",
                                INmTransportCstVal = "0",
                                INiDutLevPerc = "0",
                                INmDutLevVal = "0",
                                INcDutLevIndc = "0",
                                INiSettleDisPerc = "0",
                                INiSettleDisVal = "0",
                                INcDelivDate = string.Empty,
                                INcAuthNo = "0",
                                IsOrderConfirmation = 0
                            };

                            db.Invoices.InsertOnSubmit(invoice);
                            db.SubmitChanges();

                            foreach (var line in unh.ILD)
                            {
                                var invoiceDetail = new InvoiceDetail
                                {
                                    IDiInvoiceNumber = invoice.INID,
                                    IDcConsumerBarCode = line.ProductNumber.ProductNumberofConsumerUnit.ToString(),
                                    IDcConsumerOrdUnit = line.ProductNumber.ProductNumberofOrderUnit == null ? null : line.ProductNumber.ProductNumberofOrderUnit.ToString(),
                                    IDcProdDescription = line.ProductNumber.ProductDescription,
                                    IDcUnitOfMeasure = line.QuantityDelivered.UnitOfMeasure == null ? null : line.QuantityDelivered.UnitOfMeasure,
                                    IDiQty = line.QuantityDelivered.NumberOfUnitsDelivered.ToString(),
                                    IDmListCost = line.CostPriceExclVat.CostPrice.ToString(),
                                    IDcAdjIndicator1 = line.CreditAdjustment.Count > 0 && line.CreditAdjustment[0].Indicator != null ? line.CreditAdjustment[0].Indicator : "T1",
                                    IDmAdjValue1 = line.CreditAdjustment.Count > 0 && line.CreditAdjustment[0].Value != null ? line.CreditAdjustment[0].Value.ToString() : "0",
                                    IDiAdjPerc1 = line.CreditAdjustment.Count > 0 && line.CreditAdjustment[0].Percentage != null ? line.CreditAdjustment[0].Percentage.ToString() : "0",
                                    IDcAdjIndicator2 = line.CreditAdjustment.Count > 1 && line.CreditAdjustment[1].Indicator != null ? line.CreditAdjustment[1].Indicator : "T2",
                                    IDmAdjValue2 = line.CreditAdjustment.Count > 1 && line.CreditAdjustment[1].Value != null ? line.CreditAdjustment[1].Value.ToString() : "0",
                                    IDiAdjPerc2 = line.CreditAdjustment.Count > 1 && line.CreditAdjustment[1].Percentage != null ? line.CreditAdjustment[1].Indicator : "0",
                                    IDmNettValue = line.NetExtendedLineCostExclVat.ToString(),
                                    IDiVatPerc = line.VatRatePercentage.ToString(),
                                    IDcVatCode = line.VatRateCode
                                };
                                db.InvoiceDetails.InsertOnSubmit(invoiceDetail);
                            }



                            IEnumerable<Claim> claimList = (from c in db.Claims
                                                            where c.CLcInvoiceNumber.Contains(invoiceNumber.TrimStart('0'))
                                                                && c.CLiDCID == dcId
                                                                && c.CLcStoreEAN == storeEan
                                                                && c.CLcSupplierEAN == supplierEan
                                                            select c).ToArray();
                            Claim claim = null;
                            foreach (Claim clm in claimList)
                                if (clm.CLcInvoiceNumber.TrimStart('0') == invoiceNumber)
                                {
                                    claim = clm;

                                    break;
                                }

                            if (claim != null)
                            {
                                claim.CLiInvoiceID = invoice.INID;
                                DateTime.TryParse(invoice.INdInvoiceDate, out DateTime parsedInvoiceDate);
                                if (parsedInvoiceDate < DateTime.Now.AddYears(-10))
                                {
                                    claim.CLdInvoiceDate = null;
                                }
                                else
                                {
                                    claim.CLdInvoiceDate = parsedInvoiceDate;
                                }
                            }


                            db.SubmitChanges();

                        }
                    }

                    ts.Complete();
                    response.ResponseResult = ResponseResult.Success;
                    response.ResponseMessage = "Invoice(s) saved to DB";
                    response.Trace.Add("Invoice(s) added");
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
