using System;
using System.Transactions;
using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels.CreditNotes.CreditNoteI;
using System.Linq;

namespace Spar.Gateway.CreditNoteI
{
    public class CreditNoteIGateway
    {
        public Response CustomException(Response response, string message)
        {
            response.ResponseResult = ResponseResult.Failure;
            response.ResponseMessage = "Credit note failed to save";
            response.Trace.Add(message);

            return response;
        }
        public Response Save(string data, string uniqueIdentifier, Response response)
        {
            using (var ts = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {

                var linqXml = File.Parse(data);

                foreach (var interchange in linqXml.Interchange)
                {
                    var messageDetail = interchange.Header.MESSAGE_HEADER.Message_Detail;
                    var LDS = messageDetail.DCF.LDS;

                    using (SparDSHCreditNotesDataContext db = new SparDSHCreditNotesDataContext())
                    {
                        var messageNumber = messageDetail.REF.REFC010_REFERENCES.Reference_number.ToString();
                        var supplierEan = messageDetail.SAP.Supplier_Accounting_Point.ToString();
                        var storeEan = messageDetail.CLO.Customer_Delivery_Invoice_Point.ToString();
                        var dcEan = interchange.Header.Recipient_Identification.ToString();
                        var creditNoteNumber = messageDetail.REF.REFC010_REFERENCES.Reference_number;
                        var despatchPoint = messageDetail.SDP.Supplier_Despatch_Point.ToString();
                        var storeName = messageDetail.CLO.Customer_Delivery_Point_Name.ToString();
                        //var storeAddress = messageDetail.CLO.CUSTOMER_DELIVERY_POINT_ADDRESS.ToString();

                        var invoiceNumber = string.Empty;
                        DateTime? invoiceDate = null;
                        var claimNumber = string.Empty;
                        DateTime? claimDate = null;
                        var manualClaimNumber = string.Empty;
                        DateTime? manualClaimDate = null;


                        foreach (var reference in messageDetail.DCF.DOCUMENT_REFERENCE)
                        {
                            DateTime.TryParse(reference.Date, out DateTime referenceDate);
                            switch (reference.Document_type)
                            {

                                case "I":
                                    {
                                        invoiceNumber = reference.Reference_number;
                                        if (referenceDate < DateTime.Now.AddYears(-10))
                                            invoiceDate = null;
                                        else
                                            invoiceDate = referenceDate;

                                        break;
                                    }
                                case "C":
                                    {
                                        claimNumber = reference.Reference_number;
                                        if (referenceDate < DateTime.Now.AddYears(-10))
                                            claimDate = null;
                                        else
                                            claimDate = referenceDate;

                                        break;
                                    }

                                case "J":
                                    {
                                        manualClaimNumber = reference.Reference_number;
                                        if (referenceDate < DateTime.Now.AddYears(-10))
                                            claimDate = null;
                                        else
                                            claimDate = referenceDate;

                                        break;
                                    }
                            }
                        }

                        var creditNoteDuplicateCheck = (from cn in db.CreditNotes
                                                        where cn.CNcCreditNoteNumber == creditNoteNumber
                                                        && cn.CNcDCEAN == dcEan
                                                        && cn.CNcStoreEAN == storeEan
                                                        && cn.CNcSupplierEAN == supplierEan
                                                        && cn.DespatchPoint == despatchPoint
                                                        select cn).FirstOrDefault();

                        if (linqXml.Interchange.Count > 1 && creditNoteDuplicateCheck != null)
                        {
                            response.Trace.Add(string.Format("Credit note {0} already exists", creditNoteNumber));
                            continue;
                        }
                        else if (creditNoteDuplicateCheck != null)
                        {
                            return CustomException(response, "Credit note already exists");
                        }





                        var supplierId = 0;
                        if (String.IsNullOrEmpty(despatchPoint))
                        {
                            supplierId = (from sp in db.Suppliers
                                          where sp.SPcEANNumber == supplierEan
                                          select sp.SPID).FirstOrDefault();
                        }
                        else
                        {
                            supplierId = (from sp in db.Suppliers
                                          where sp.SPcEANNumber == despatchPoint
                                          select sp.SPID).FirstOrDefault();
                        }
                        if (linqXml.Interchange.Count > 1 && supplierId == 0)
                        {
                            response.Trace.Add(string.Format("Supplier for credit note {0} does not exists", creditNoteNumber));
                            continue;
                        }
                        else if (supplierId == 0)
                        {
                            return CustomException(response, "Supplier does not exist");
                        }
                        var dcId = (from d in db.DCs
                                    where d.DCcEANNumber == dcEan
                                    select d.DCID).FirstOrDefault();
                        if (linqXml.Interchange.Count > 1 && dcId == 0)
                        {
                            response.Trace.Add(string.Format("DC for credit note {0} does not exists", creditNoteNumber));
                            continue;
                        }
                        if (dcId == 0)
                        {
                            return CustomException(response, "DC does not exist");
                        }
                        var store = (from st in db.Stores
                                     where st.STcEANNumber == storeEan
                                        && st.STiDCID == dcId
                                     select new
                                     {
                                         Id = st.STID,
                                         Code = st.STcCode
                                     }).FirstOrDefault();

                        if (linqXml.Interchange.Count > 1 && (store == null))
                        {
                            response.Trace.Add(string.Format("Store for credit note {0} does not exists", creditNoteNumber));
                            continue;
                        }
                        if (store == null)
                        {
                            var messageException = new MessageException
                            {
                                MEcMessageNr = messageNumber,
                                MEcType = "CREDIT",
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

                            return CustomException(response, "Store does not exist");
                        }

                        var invoiceId = 0;
                        if (!String.IsNullOrEmpty(invoiceNumber))
                        {
                            var invoice = (from i in db.Invoices
                                           where i.INcInvoiceNumber == invoiceNumber
                                            && i.INcSupplierEAN == supplierEan
                                            && i.INcStoreEAN == storeEan
                                            && i.INcDCEAN == dcEan
                                           select new { Id = i.INID, Date = i.INdInvoiceDate }).FirstOrDefault();



                            if (invoice != null)
                            {
                                invoiceId = invoice.Id;

                                DateTime.TryParse(invoice.Date, out DateTime parsedInvoiceDate);
                                if (parsedInvoiceDate < DateTime.Now.AddYears(-10))
                                {
                                    invoiceDate = null;
                                }
                                else
                                {
                                    invoiceDate = parsedInvoiceDate;
                                }
                            }
                        }

                        var claimId = 0;
                        if (!String.IsNullOrEmpty(claimNumber))
                        {
                            var claim = (from c in db.Claims
                                         where c.CLcClaimNumber == claimNumber
                                          && c.CLcSupplierEAN == supplierEan
                                          && c.CLcStoreEAN == storeEan
                                          && c.CLcDCEAN == dcEan
                                         select new { Id = c.CLID, Date = c.CLdClaimDate }).FirstOrDefault();

                            if (claim != null)
                            {
                                claimId = claim.Id;
                                claimDate = claim.Date;
                            }

                        }

                        var creditNoteAcknowledgement = (from cna in db.CreditNoteAcknowledgements
                                                         where cna.IsProcessed == false
                                                             && cna.CreditNoteNumber == creditNoteNumber
                                                             && cna.SupplierEan == supplierEan
                                                             && cna.StoreEan == storeEan
                                                             && cna.DcEan == dcEan
                                                         select cna).FirstOrDefault();


                        DateTime? acknowledgedByDc = null;
                        if (creditNoteAcknowledgement != null)
                        {
                            acknowledgedByDc = creditNoteAcknowledgement.AcknowledgedByDc;
                            creditNoteAcknowledgement.IsProcessed = true;
                            creditNoteAcknowledgement.ProcessedDateTime = DateTime.Now;
                        }


                        var isForceCredit = false;
                        var isReward = false;
                        var isStamp = false;
                        string creditIndicator = null;
                        creditIndicator = messageDetail.CLO.Alternate_Invoice_Point.ToString();
                        if (creditIndicator.Split('|').Length > 1)
                        {
                            creditIndicator = creditIndicator.Split('|')[1];
                        }

                        switch (creditIndicator)
                        {
                            case "FC":
                                {
                                    isForceCredit = true;
                                    break;
                                }
                            case "R":
                                {
                                    isReward = true;
                                    break;

                                }
                            case "S":
                                {
                                    isStamp = true;
                                    break;
                                }
                        }


                        var creditNote = new CreditNote
                        {
                            CNiStoreID = store.Id,
                            CNcStoreEAN = storeEan,
                            CNiDCID = dcId,
                            CNcDCEAN = dcEan,
                            CNiSupplierID = supplierId,
                            CNcSupplierEAN = supplierEan,
                            CNcCreditNoteNumber = creditNoteNumber,
                            CNdCreditNoteDate = Convert.ToDateTime(messageDetail.REF.REFC010_REFERENCES.Date),
                            DespatchPoint = despatchPoint,
                            CNdReceivedDate = DateTime.Now,
                            CNdTransDate = DateTime.Now,
                            CNdPostDate = DateTime.Now,
                            CNdRecDCBackDate = creditNoteAcknowledgement?.AcknowledgedByDc,
                            IsForceCredit = isForceCredit,
                            IsReward = isReward,
                            IsStamp = isStamp,
                            CNmTotCostExcl = (decimal)messageDetail.BTT.Batch_Total_Amount_Excluding_Vat,
                            CNmVat = (decimal)messageDetail.BTT.Batch_Total_Of_Vat_Amount,
                            CNmTotCostIncl = (decimal)messageDetail.BTT.Batch_Total_Amount_Payable_Including,


                        };
                        db.CreditNotes.InsertOnSubmit(creditNote);
                        db.SubmitChanges();

                        var creditNoteClaim = new CreditNoteClaim
                        {
                            CCiCreditNoteID = creditNote.CNID,
                            CCiNumLines = LDS.Count(),
                            CCiClaimID = claimId,
                            CCdClaimDate = claimDate,
                            CCcClaimNumber = claimNumber,
                            CCiInvID = invoiceId,
                            CCdInvDate = invoiceDate,
                            CCcInvNumber = invoiceNumber,
                            CCcManualNum = manualClaimNumber,
                            CCdManualDate = manualClaimDate,
                            CCmTotCostExcl = (decimal)messageDetail.BTT.Batch_Total_Amount_Excluding_Vat,
                            CCmTotVat = (decimal)messageDetail.BTT.Batch_Total_Of_Vat_Amount,
                            CCmTotCostIncl = (decimal)messageDetail.BTT.Batch_Total_Amount_Payable_Including,

                        };
                        db.CreditNoteClaims.InsertOnSubmit(creditNoteClaim);
                        db.SubmitChanges();


                        var codeList5 = (from cl in db.CodeList5s
                                         select cl);

                        foreach (var line in LDS)
                        {
                            var reasonCode = "GR";
                            var reasonId = (from cl in codeList5
                                            where cl.CVcCode == reasonCode
                                            select cl.CVID).FirstOrDefault();

                            var productCode = line.PRODUCT_NUMBER.Supplier_product_code.ToString();

                            var creditNoteClaimLine = new CreditNoteClaimLine
                            {
                                CMiCreditNoteClaimID = creditNoteClaim.CCID,
                                CMiLineSeqNum = line.Line_Sequence_Number,
                                CMcReasonCode = reasonCode,
                                CMcGoodsRetCode = string.Empty,
                                CMmNetCost = (decimal)line.Net_Extended_Line_Cost_Excluding_Vat * (1 + (line.Vat_Rate_Percentage / 100)),
                                CMiVatPerc = (int)line.Vat_Rate_Percentage,
                                CMmVatPerc = line.Vat_Rate_Percentage,
                                CMcVatCode = line.Vat_Rate_Code,
                                CMcProdCode = productCode,
                                CMcOrderUnit = line.PRODUCT_NUMBER.EAN_product_number.ToString(),
                                CMiNumUnits = (int)line.QUANTITY_DETAILS.Number_of_units_involved,
                                CMcUOM = line.QUANTITY_DETAILS.Unit_of_measure == null ? string.Empty : line.QUANTITY_DETAILS.Unit_of_measure,
                                CMcProdDescr = line.PRODUCT_NUMBER.Product_description,
                                CMmTotLineExcl = (decimal)line.Net_Extended_Line_Cost_Excluding_Vat,
                                CMiReasonID = reasonId,
                                CMmCostPrice = (decimal)line.COST_PRICE.Cost_price,



                            };
                            db.CreditNoteClaimLines.InsertOnSubmit(creditNoteClaimLine);
                        }


                        var claimForUpdate = db.Claims.Where(c => c.CLID == claimId).FirstOrDefault();
                        if (claimForUpdate != null)
                        {
                            claimForUpdate.CLiCNID = creditNote.CNID;
                            claimForUpdate.CLdCNDate = creditNote.CNdCreditNoteDate;
                            claimForUpdate.CLiInvoiceID = invoiceId;
                            claimForUpdate.CLdInvoiceDate = invoiceDate;
                        }



                        db.SubmitChanges();
                        var result = db.MatchCreditNoteToClaim(creditNote.CNID);
                    }
                }

                ts.Complete();

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "Credit note(s) saved to DB";
                response.Trace.Add("Credit note(s) added");
            }
            return response;
        }
    }
}