using BizToDBIntegration.WcfContracts;
using System;
using System.Transactions;
using System.Linq;
using Spar.Data.XsdModels.Claims;

namespace Spar.Gateway.Claim
{
    public class ClaimGateway
    {
        public Response CustomException(Response response, string message)
        {
            response.ResponseResult = ResponseResult.Failure;
            response.ResponseMessage = "Claim(s) failed to save";
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
                    using (SparDSHDataContextDataContext db = new SparDSHDataContextDataContext())
                    {
                        db.CommandTimeout = 600;

                        var linqXml = Claims.Parse(data);

                        foreach (var c in linqXml.Claim)
                        {
                            var claimNumber = c.REF.ClaimNum.ClaimNumber;
                            var storeEan = c.CLO.CustomerDeliveryPoint;
                            var storeName = c.CLO.CustomerDeliveryPointName;
                            //var storeAddress = c.CLO.CustomerDeliveryPointAddress.ToString();
                            var supplierEan = c.SAP.SupplierAccountingPoint;
                            var dcEan = c.CLO.CustomerOrderPoint;
                            var despatchPoint = c?.SDP?.SupplierDespatchPoint;
                            var claimType = c.REF.ContractDealNumber.CustomersRepresentative;
                            var originalClaimType = claimType;
                            var claimStatusId = 1;
                            var user = "SYSTEM USER";
                            int statusChangedByUserId = 0;
                            var supplierId = 0;
                            var invoiceNumber = c.CRF.ClaimReference.InvNum;
                            DateTime? invoiceDate = c.CRF.ClaimReference.InvDate;
                            var creditNoteNumber = c.CRF.ClaimReference.CreditNum;
                            var creditNoteDate = c.CRF.ClaimReference.CreditDate;
                            var claimTypeId = 0;
                            var claimReferenceType = c.REF.ContractDealNumber.ContractDealNrCustomer;
                            var manualClaimNumber = c.REF.ContractDealNumber.ContractDealNrSupplier;
                            DateTime claimDate = c.REF.ClaimNum.ClaimDate;
                            DateTime recievedDate = claimDate;
                            DateTime transDate = claimDate;
                            DateTime? extractDate = null;
                            DateTime? receivedDate = DateTime.Now;
                            var isCapturedClaimByTypeLength = originalClaimType.Length > 3 || originalClaimType == "VCL" ? true : false;
                            var claimReasonId = 0;
                            DateTime? manualClaimDate = null;
                            int? claimSubCategoryId = null;
                            int? claimSubReasonId = null;

                            var reasonCode = string.Empty;
                            if (c.CLN.Count() > 0)
                            {
                                reasonCode = c.CLN[0].LineReasonCode?.LineReasonCode ?? string.Empty;
                            }

                            decimal? claimAmount = c.CMS.TotalAmountPayableIncVat;
                            decimal? claimVat = c.CMS.TotalVatAmountPayable;
                            var numberOfLines = c.CLN.Count();
                            var narratives = c.REF.ContractDealNumber?.SuppliersRepresentative;
                            var lastUpdated = DateTime.Now;
                            var discPerc1 = 0;
                            var discAmt1 = 0;
                            var discPerc2 = 0;
                            var discAmt2 = 0;
                            var discPerc3 = 0;
                            var discAmt3 = 0;

                            var claimCategoryId = 0;

                            var messageNumber = c.Header.InterchangeNo.ToString();

                            claimTypeId = GetClaimTypeId(originalClaimType);

                            if (claimReferenceType != "RFC")
                            {
                                claimReferenceType = "DFC";
                            }

                            // DC exists?
                            var dc = db.DCs.Where(d => d.DCcEANNumber == dcEan).FirstOrDefault();
                            if (dc == null)
                            {
                                response.Trace.Add(string.Format("Claim {0} DC does not exist", claimNumber));
                                return CustomException(response, "DC does not exist");
                            }

                            // Store exists?
                            var store = db.Stores.Where(s => s.STiDCID == dc.DCID && s.STcEANNumber == storeEan).FirstOrDefault();
                            if (store == null)
                            {
                                var messageException = new MessageException
                                {
                                    MEcMessageNr = messageNumber,
                                    MEcType = "CLAIM",
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

                                response.Trace.Add(string.Format("Claim {0} store does not exist", claimNumber));
                                return CustomException(response, "store does not exist");


                            }







                            if (originalClaimType != "VCL")
                            {
                                if (!string.IsNullOrEmpty(despatchPoint))
                                {
                                    supplierId =
                                        db.Suppliers.Where(
                                            s => s.SPcEANNumber == despatchPoint
                                            ).FirstOrDefault().SPID;
                                }
                                else
                                {
                                    supplierId =
                                        db.Suppliers.Where(
                                            s => s.SPcEANNumber == supplierEan
                                            ).FirstOrDefault().SPID;
                                }
                                if (supplierId == 0)
                                {
                                    response.Trace.Add(string.Format("Claim {0} supplier does not exist", claimNumber));
                                    return CustomException(response, "Supplier does not exist");
                                }
                            }
                            else
                            {
                                isCapturedClaimByTypeLength = true;
                            }


                            var isClaimExisting = db.Claims.Where(ec => ec.CLcClaimNumber == claimNumber
                                 && ec.CLiDCID == dc.DCID
                                 && ec.CLiStoreID == store.STID
                                 && ec.CLiSupplierID == supplierId
                                 && ec.CLcSupplierEAN == supplierEan
                                 && Equals(ec.DespatchPoint, despatchPoint)
                                 ).Count() > 0 ? true : false;

                            if (isClaimExisting)
                            {
                                response.Trace.Add(string.Format("Claim {0} already exists", claimNumber));
                                return CustomException(response, "Claim already exist");
                            }



                            var claimCategoryReason = from category in db.ClaimCategories
                                                      from reason in db.ClaimReasons
                                                        .Where(r => category.ClaimCategoryId == r.ClaimCategoryId)
                                                        .DefaultIfEmpty()
                                                      where reason.ReasonCode == reasonCode
                                                         && category.ActiveInactiveIndicator == true
                                                         && category.ClaimTypeID == claimTypeId
                                                      select new
                                                      {
                                                          category.ClaimCategoryId,
                                                          reason.ClaimReasonId,
                                                          category.CategoryTypeId
                                                      };



                            if (isCapturedClaimByTypeLength)
                            {
                                claimCategoryReason.Where(x => x.ClaimCategoryId == claimCategoryId);
                                claimStatusId = 3;
                            }

                            if (originalClaimType == "SCL" && reasonCode != "UN")
                            {
                                claimCategoryReason.Where(ct => ct.CategoryTypeId == 1 || ct.CategoryTypeId == 3);
                            }


                            if (originalClaimType == "BCL"
                                || originalClaimType == "DCL"
                                || claimType.Length > 3)
                            {
                                claimStatusId = 3;
                                claimReasonId = 0;
                                claimCategoryId = 0;

                                if (originalClaimType == "BCL")
                                {
                                    claimReasonId = 0;
                                }
                            }
                            else if (originalClaimType == "VCL")
                            {
                                claimStatusId = 29;
                                supplierId = db.GetVendorSupplierDetails(supplierEan).FirstOrDefault().SPID;
                                if (supplierId == 0)
                                {
                                    response.Trace.Add(string.Format("Claim {0} supplier does not exist", claimNumber));
                                    return CustomException(response, "Supplier does not exist");
                                }
                            }
                            else if (originalClaimType == "WCL")
                            {
                                IsCapturedClaimResult isCapturedClaim = db.IsCapturedClaim(claimNumber, user).FirstOrDefault();
                                if (isCapturedClaim.Column1 == 1)
                                {
                                    var isWarehouseUser = db.Users.Where(u => u.UScUserName == user
                                        && u.USiType == 3).Count() > 0
                                        ? true : false;

                                    if (isWarehouseUser)
                                    {
                                        claimStatusId = 3;
                                    }
                                    else
                                    {
                                        claimStatusId = 29;
                                    }
                                }



                                if (dcEan == "6004930005184")
                                {
                                    var northRandStoreExists = (from s in db.Stores
                                                                where s.STcEANNumber == storeEan
                                                                && s.STiDCID == 2
                                                                && s.STiIsLive == 1
                                                                select s).Count() > 0 ? true : false;

                                    if (northRandStoreExists)
                                    {
                                        dcEan = "6001008999925";
                                        supplierEan = "6001008999925";
                                    }
                                }

                            }



                            var invoiceId = 0;
                            if (!string.IsNullOrEmpty(invoiceNumber))
                            {
                                Invoice invoice;
                                invoice = db.Invoices.Where(i => i.INcInvoiceNumber == invoiceNumber
                                                && i.INcSupplierEAN == supplierEan
                                                && i.INcStoreEAN == storeEan).FirstOrDefault();

                                if (invoice != null)
                                {
                                    invoiceId = invoice.INID;
                                }
                            };

                            var creditNoteId = 0;
                            if (!string.IsNullOrEmpty(creditNoteNumber))
                            {
                                CreditNote creditNote;
                                creditNote = db.CreditNotes.Where(cn => cn.CNcCreditNoteNumber == creditNoteNumber
                                        && cn.CNcSupplierEAN == supplierEan
                                        && cn.CNcSupplierEAN == storeEan).FirstOrDefault();

                                if (creditNote != null)
                                {
                                    creditNoteId = creditNote.CNID;
                                }
                            };




                            var ccr = claimCategoryReason.FirstOrDefault();

                            var claim = new Claim
                            {
                                CLiStoreID = store.STID,
                                CLcStoreEAN = store.STcEANNumber,
                                CLiDCID = dc.DCID,
                                CLcDCEAN = dcEan,
                                CLiSupplierID = supplierId,
                                CLcSupplierEAN = supplierEan,
                                CLcClaimNumber = claimNumber,
                                CLdClaimDate = claimDate,
                                CLdReceivedDate = recievedDate,
                                CLdTransDate = transDate,
                                CLdExtractDate = extractDate,
                                CLcClaimType = claimReferenceType,
                                CLiClaimNumberType = null,
                                CLiInvoiceID = invoiceId,
                                CLcInvoiceNumber = invoiceNumber,
                                CLdInvoiceDate = invoiceDate,
                                CLcManualClaimNum = manualClaimNumber,
                                CLdManualClaimDate = manualClaimDate,
                                CLiCNID = creditNoteId,
                                CLcCNNumber = creditNoteNumber,
                                CLdCNDate = creditNoteDate,
                                CLmAmount = claimAmount,
                                CLmVat = claimVat,
                                CLcDiscIndc1 = "T1",
                                CLiDiscPerc1 = discPerc1, 
                                CLmDiscAmt1 = discAmt1,  
                                CLcDiscIndc2 = "T2",
                                CLiDiscPerc2 = discPerc2, 
                                CLmDiscAmt2 = discAmt2,   
                                CLcDiscIndc3 = "T3",
                                CLiDiscPerc3 = discPerc3,
                                CLmDiscAmt3 = discAmt3,
                                CLiReasonID = ccr?.ClaimReasonId,
                                CLiNumLines = numberOfLines,
                                CLcNarratives = narratives,
                                StatusId = claimStatusId,
                                TypeId = claimTypeId,
                                ReceivedDate = receivedDate,
                                LastUpdated = lastUpdated,
                                ClaimCategoryId = ccr?.ClaimCategoryId,
                                OriginalClaimType = originalClaimType,
                                DespatchPoint = despatchPoint,
                                ReasonCode = reasonCode
                            };

                            db.Claims.InsertOnSubmit(claim);
                            db.SubmitChanges();

                            var cl = c.CLN;
                            foreach (var cLN in c.CLN)
                            {

                                var goodsReturnReason = cLN.LineReasonCode?.LineGoodsReturnedBasis;

                                var claimDetailReasonId = ccr?.ClaimReasonId == 0 || ccr?.ClaimReasonId == null ? 98 : ccr?.ClaimReasonId;
                                var goodsReturnId = (from csr in db.ClaimSubReasons
                                                     join cr in db.ClaimReasons
                                                         on csr.ClaimReasonId equals cr.ClaimReasonId
                                                     join cc in db.ClaimCategories
                                                         on cr.ClaimCategoryId equals cc.ClaimCategoryId
                                                     where cc.ClaimCategoryId == claimCategoryId
                                                         && csr.Code == goodsReturnReason
                                                         && cc.ClaimTypeID == claimTypeId
                                                     select csr.ClaimSubReasonId
                                                    ).FirstOrDefault();


                                var claimDetail = new ClaimDetail
                                {
                                    CDiClaimID = claim.CLID,
                                    CDcProdCode = cLN.ProductNumber?.SupplierProductCode ?? string.Empty,
                                    CDcProdEAN = cLN.ProductNumber?.EanProductNumberConsumerUnit,
                                    CDcProdDescr = cLN.ProductNumber?.ProductDescription,
                                    CDiConsumerUnit = Convert.ToInt32(cLN.CostPrice?.ConsumerUnitsPerCostPrice),
                                    CDcUOM = null,
                                    CDiQtyWhole = Convert.ToInt32(cLN.QuantityDetails?.NumberOfUnitsClaimed),
                                    CDiQtyLoose = Convert.ToInt32(cLN.QuantityDetails?.LooseConsumerUnitsReturned),
                                    CDmUnitPrice = cLN.CostPrice?.CostPriceExVat,
                                    CDiTotMeasure = null,
                                    CDcDealInd1 = cLN.CreditAdjustments.AdjustmentIndicator[0],
                                    CDiDealPerc1 = Convert.ToDouble(cLN.CreditAdjustments.Percentage[0]),
                                    CDmDealAmt1 = cLN.CreditAdjustments.Value[0],
                                    CDcDealInd2 = cLN.CreditAdjustments.AdjustmentIndicator[1],
                                    CDiDealPerc2 = Convert.ToDouble(cLN.CreditAdjustments.Percentage[1]),
                                    CDmDealAmt2 = cLN.CreditAdjustments.Value[1],
                                    CDmVatPerc = Convert.ToInt32(cLN.VatRatePercentage),
                                    CDmSubTot = cLN.LineSubTotalExVat,
                                    CDiClaimReasonID = claimDetailReasonId,
                                    CDiGoodsRetID = goodsReturnId,
                                    CDcNarr1 = $"{cLN.ContractDealNumber.SuppliersRepresentative} {cLN?.LineNarratives?.Narrative?.NarrativeDescription ?? string.Empty}",
                                    CDcNarr2 = null,
                                    CDcNarr3 = null
                                };

                                db.ClaimDetails.InsertOnSubmit(claimDetail);
                            }


                            db.SubmitChanges();

                            db.AddToClaimsAuditLog(claim.CLID, claimStatusId, statusChangedByUserId, narratives, null, null, "S", user, true,
                           false, null, claimCategoryId, claimSubCategoryId, claimReasonId, 0, 0, claimSubReasonId, null, null,
                           null, null, null, null, null, null, null);

                            db.SubmitChanges();

                            db.editClaim(claim.CLID, narratives, "T3", discPerc3, discAmt3);




                        }
                    }
                }
                catch (Exception e)
                {
                    response.ResponseResult = ResponseResult.Exception;
                    response.ResponseMessage = "Claim(s) failed writing to DB";
                    response.Trace.Add(e.Message);
                    return response;
                }

                ts.Complete();
                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "Claim(s) processed";
                return response;
            }

        }



        private int GetClaimTypeId(string originalClaimType)
        {
            switch (originalClaimType)
            {
                case "SCL":
                    return 1;
                case "DCL":
                    return 2;
                case "WCL":
                    return 3;
                case "BCL":
                    return 4;
                case "VCL":
                    return 5;
                default:
                    return 0;
            }

        }
    }
}
