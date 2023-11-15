using BizToDBIntegration.WcfContracts;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Transactions;

namespace Spar.Gateway.RemittanceAdvice
{
    internal sealed class RemittanceAdviceGateway
    {
        public Response CreateRA(string xmlData, Response returnResponse)
        {
            string xml = xmlData.ToString();

            try
            {
                var raLinqXml = remittancedocumentV2.Parse(xml);
                var raType = (from raXml in raLinqXml.remittanceadvice
                              select raXml.ratype).First();

                //This assumes that RAHeader will always be a single element in the XML
                var remittanceAdvice = (from rxml in raLinqXml.remittanceadvice
                                        from headers in rxml.raheader
                                        select headers).SingleOrDefault();
                RemittanceAdvice remittanceAdviceRecord = new RemittanceAdvice
                {
                    RAType = raType.ToString().Trim(),
                    DCEanCode = remittanceAdvice.dceancode.Trim(),
                    DCName = remittanceAdvice.dcname.Trim(),
                    DCPostalAddress1 = remittanceAdvice.dcpostaladdress1.Trim(),
                    DCPostalAddress2 = remittanceAdvice.dcpostaladdress2.Trim(),
                    DCPostalCode = remittanceAdvice.dcpostalcode.Trim(),
                    SupplierEanLocationCode = remittanceAdvice.suppliereanlocationcode.Trim(),
                    SupplierEanDispatchPoint = remittanceAdvice.suppliereandispatchpoint.Trim(),
                    DCVatRegNo = remittanceAdvice.dcvatregno.Trim(),
                    SupplierNumber = remittanceAdvice.suppliernumber.Trim(),
                    SupplierName = remittanceAdvice.suppliername.Trim(),
                    SupplierAddress1 = remittanceAdvice.supplieraddress1.Trim(),
                    SupplierAddress2 = remittanceAdvice.supplieraddress2.Trim(),
                    SupplierAddress3 = remittanceAdvice.supplieraddress3.Trim(),
                    SupplierPostalCode = remittanceAdvice.supplierpostalcode.Trim(),
                    SupplierVatRegNo = remittanceAdvice.suppliervatregno.Trim(),
                    PaymentNumber = remittanceAdvice.paymentnumber.Trim(),
                    ExtractedDate = Convert.ToDateTime(remittanceAdvice.extracteddate.Trim()),
                    RADate = Convert.ToDateTime(remittanceAdvice.radate.Trim()),
                    TermsCode = remittanceAdvice.termscode.Trim(),
                    TermsDays = Convert.ToInt32(remittanceAdvice.ttermsdays.Trim()),
                    CreatedAt = DateTime.Now,
                    Old = false
                };

                var details = from rxml in raLinqXml.remittanceadvice
                              from rxmlDetails in rxml.radetails
                              select rxmlDetails;

                foreach (var detail in details)
                {
                    RemittanceAdviceDetail detailRecord = new RemittanceAdviceDetail
                    {
                        DocumentDate = Convert.ToDateTime(detail.docdate.Trim()),
                        DocumentType = detail.doctype.Trim(),
                        SupplierDocumentNo = detail.supplierdocno.Trim(),
                        StoreCode = detail.storecode.Trim(),
                        StoreName = detail.storename.Trim(),
                        StoreEanCode = detail.storeeancode.Trim(),
                        DocumentAmountExcl = Convert.ToDecimal(detail.documentamountexcl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        DocumentAmountIncl = Convert.ToDecimal(detail.documentamountincl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        DiscountAmountExcl = Convert.ToDecimal(detail.discountamountexcl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        DiscountAmountIncl = Convert.ToDecimal(detail.discountamountincl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        VatAmountPayable = Convert.ToDecimal(detail.vatamountpayable.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        NetAmountIncl = Convert.ToDecimal(detail.netamountincl.Trim(), System.Globalization.CultureInfo.InvariantCulture)
                    };
                    remittanceAdviceRecord.RemittanceAdviceDetails.Add(detailRecord);
                }

                var totalPayments = from rxml in raLinqXml.remittanceadvice
                                    from rxmlTotalPayment in rxml.totalforpayment
                                    select rxmlTotalPayment;

                foreach (var totalPayment in totalPayments)
                {
                    RemittanceAdviceTotalPayment totalPaymentRecord = new RemittanceAdviceTotalPayment
                    {
                        Description = totalPayment.totaldescription.Trim(),
                        DiscountAmountExcl = Convert.ToDecimal(totalPayment.totaldiscountamountexcl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        DiscountAmountIncl = Convert.ToDecimal(totalPayment.totaldiscountamountincl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        DocumentAmountExcl = Convert.ToDecimal(totalPayment.totaldocumentamountexcl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        DocumentAmountIncl = Convert.ToDecimal(totalPayment.totaldocumentamountincl.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        VatAmountPayable = Convert.ToDecimal(totalPayment.totalvatamountpayable.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        NetAmountIncl = Convert.ToDecimal(totalPayment.totalnetamountincl.Trim(), System.Globalization.CultureInfo.InvariantCulture)
                    };
                    remittanceAdviceRecord.RemittanceAdviceTotalPayments.Add(totalPaymentRecord);
                }

                var otherTotals = from rxml in raLinqXml.remittanceadvice
                                  from rxmlOtherTotal in rxml.othertotal
                                  select rxmlOtherTotal;

                foreach (var otherTotal in otherTotals)
                {
                    RemittanceAdviceOtherTotal otherTotalRecord = new RemittanceAdviceOtherTotal
                    {
                        Description = otherTotal.totaldescription.Trim(),
                        Amount = Convert.ToDecimal(otherTotal.amount.Trim(), System.Globalization.CultureInfo.InvariantCulture)
                    };
                    remittanceAdviceRecord.RemittanceAdviceOtherTotals.Add(otherTotalRecord);
                }

                var taxInvoices = from rxml in raLinqXml.remittanceadvice
                                  from rxmlTaxInvoice in rxml.taxinvoice
                                  select rxmlTaxInvoice;

                foreach (var taxInvoice in taxInvoices)
                {
                    var invoiceId = Guid.NewGuid();
                    RemittanceAdviceTaxInvoice taxInvoiceRecord = new RemittanceAdviceTaxInvoice
                    {
                        Id = invoiceId,
                        DocumentNumber = taxInvoice.documentnumber.Trim(),
                    };

                    foreach (var otherTax in taxInvoice.othertaxdoctotal)
                    {
                        RemittanceAdviceOtherTax taxInvoiceOtherTaxRecord = new RemittanceAdviceOtherTax
                        {
                            TotalDescription = otherTax.totaldescription.Trim(),
                            Amount = Convert.ToDecimal(otherTax.amount.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        };
                        taxInvoiceRecord.RemittanceAdviceOtherTaxes.Add(taxInvoiceOtherTaxRecord);
                    }
                    remittanceAdviceRecord.RemittanceAdviceTaxInvoices.Add(taxInvoiceRecord);
                }

                var taxCreditNotes = from rxml in raLinqXml.remittanceadvice
                                     from rxmlTaxCreditNote in rxml.taxcreditnote
                                     select rxmlTaxCreditNote;

                foreach (var taxCreditNote in taxCreditNotes)
                {
                    RemittanceAdviceTaxCreditNote taxCreditNoteRecord = new RemittanceAdviceTaxCreditNote
                    {
                        DocumentNumber = taxCreditNote.documentnumber.Trim(),
                    };

                    foreach (var otherTax in taxCreditNote.othertaxdoctotal)
                    {
                        RemittanceAdviceOtherTax taxCreditNoteOtherTaxRecord = new RemittanceAdviceOtherTax
                        {
                            TotalDescription = otherTax.totaldescription.Trim(),
                            Amount = Convert.ToDecimal(otherTax.amount.Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        };
                        taxCreditNoteRecord.RemittanceAdviceOtherTaxes.Add(taxCreditNoteOtherTaxRecord);
                    }
                    remittanceAdviceRecord.RemittanceAdviceTaxCreditNotes.Add(taxCreditNoteRecord);
                }

                using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required,
                        new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
                {
                    using (SparDSRemittanceAdvice db = new SparDSRemittanceAdvice())
                    {
                        var remittance = (from ra in db.RemittanceAdvices
                                          where ra.PaymentNumber == remittanceAdvice.paymentnumber
                                            && ra.DCEanCode == remittanceAdvice.dceancode
                                          select ra).ToList();

                        if (remittance.Count == 0)
                        {
                            db.RemittanceAdvices.InsertOnSubmit(remittanceAdviceRecord);

                            db.SubmitChanges(ConflictMode.FailOnFirstConflict);
                        }
                        else if (remittance.Count > 0)
                        {
                            var ra = remittance.FirstOrDefault();
                            if (ra.RemittanceAdviceTaxInvoices.FirstOrDefault() != null)
                            {
                                var raot =
                                   (from r in db.RemittanceAdviceOtherTaxes
                                    where r.RemittanceAdviceTaxInvocieId == ra.RemittanceAdviceTaxInvoices.FirstOrDefault().Id
                                    select r).DefaultIfEmpty();

                                foreach (var ti in raot)
                                    db.RemittanceAdviceOtherTaxes.DeleteOnSubmit(ti);

                                db.SubmitChanges(ConflictMode.FailOnFirstConflict);

                            }




                            if (ra.RemittanceAdviceTaxCreditNotes.FirstOrDefault() != null)
                            {
                                var x = (from r in db.RemittanceAdviceTaxCreditNotes
                                         where r.RemittanceAdviceId == ra.RemittanceAdviceTaxCreditNotes.FirstOrDefault().RemittanceAdviceId
                                         select r).DefaultIfEmpty();

                                foreach (var cn in x)
                                {
                                    var raOT = db.RemittanceAdviceOtherTaxes.Where(o => o.RemittanceAdviceTaxCreditNoteId == cn.Id).DefaultIfEmpty();
                                    foreach (var ot in raOT)
                                        db.RemittanceAdviceOtherTaxes.DeleteOnSubmit(ot);


                                    db.RemittanceAdviceTaxCreditNotes.DeleteOnSubmit(cn);
                                }

                                db.SubmitChanges(ConflictMode.FailOnFirstConflict);
                            }
                        }
                        else
                        {
                            
                            returnResponse.ResponseResult = ResponseResult.Failure;
                            returnResponse.ResponseMessage = "Transaction failed writing to DB";
                            string errorString = "Inserting Duplicate Record to Database; Payment Number: " + remittanceAdvice.paymentnumber.ToString() + " DCEanCode: " + remittanceAdvice.dceancode.ToString();
                            returnResponse.Trace.Add(errorString);
                            returnResponse.Trace.Add("Saving Remittance Advice to DB Failed");
                            return returnResponse;
                        }
                        ts.Complete();

                        returnResponse.ResponseMessage = "Transaction saved to DB";
                        returnResponse.Trace.Add("Saving Remittance Advice to DB Successful");
                        return returnResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                returnResponse.ResponseResult = ResponseResult.Exception;
                returnResponse.ResponseMessage = "Transaction failed writing to DB";
                returnResponse.Trace.Add(ex.Message);
                return returnResponse;
            }
        }

        public Response CreateOldRA(string xmlData, Response returnResponse)
        {
            string xml = xmlData.ToString();

            if (xml.ToLower().Contains("<ratype>dc</ratype>"))
            {
                if (xml.ToLower().Contains("<totalcommissiondeducteddesc>"))
                    return SaveRemittanceAdviceOldDCCommission(xml, returnResponse);
                else
                    return SaveRemittanceAdviceOldDC(xml, returnResponse);
            }
            if (xml.ToLower().Contains("<ratype>dsh</ratype>"))
            {
                return SaveRemittanceAdviceOldDS(xml, returnResponse);
            }
            //ResponseResult is a Success as the transaction should not write to a DB if duplicated
            returnResponse.ResponseResult = ResponseResult.Success;
            returnResponse.ResponseMessage = "Transaction has an invalid XML format";
            returnResponse.Trace.Add("XML does not conform to the current standards");
            return returnResponse;
        }

        public Response SaveRemittanceAdviceOldDS(string xml, Response returnResponse)
        {
            try
            {
                var raLinqXml = Spar.Data.XsdModels.RemittanceAdvice.Old.remittancedocument.Parse(xml);

                var raType = (from raXml in raLinqXml.remittanceadvice
                              select raXml.ratype).First();

                //This assumes that RAHeader will always be a single element in the XML
                var remittanceAdvice = (from rxml in raLinqXml.remittanceadvice
                                        from headers in rxml.raheader
                                        select headers).SingleOrDefault();
                RemittanceAdvice remittanceAdviceRecord = new RemittanceAdvice
                {
                    RAType = raType.ToString().Trim(),
                    DCEanCode = remittanceAdvice.dceancode.Trim(),
                    DCName = remittanceAdvice.dcname.Trim(),
                    DCPostalAddress1 = remittanceAdvice.dcpostaladdress1.Trim(),
                    DCPostalAddress2 = remittanceAdvice.dcpostaladdress2.Trim(),
                    DCPostalCode = remittanceAdvice.dcpostalcode.Trim(),
                    SupplierEanLocationCode = remittanceAdvice.suppliereanlocationcode.Trim(),
                    SupplierEanDispatchPoint = remittanceAdvice.suppliereandispatchpoint.Trim(),
                    DCVatRegNo = remittanceAdvice.dcvatregno.Trim(),
                    SupplierNumber = remittanceAdvice.suppliernumber.Trim(),
                    SupplierName = remittanceAdvice.suppliername.Trim(),
                    SupplierAddress1 = remittanceAdvice.supplieraddress1.Trim(),
                    SupplierAddress2 = remittanceAdvice.supplieraddress2.Trim(),
                    SupplierAddress3 = remittanceAdvice.supplieraddress3.Trim(),
                    SupplierPostalCode = remittanceAdvice.supplierpostalcode.Trim(),
                    SupplierVatRegNo = remittanceAdvice.suppliervatregno.Trim(),
                    PaymentNumber = remittanceAdvice.paymentnumber.Trim(),
                    ExtractedDate = Convert.ToDateTime(remittanceAdvice.extracteddate.Trim()),
                    RADate = Convert.ToDateTime(remittanceAdvice.radate.Trim()),
                    TermsCode = remittanceAdvice.termscode.Trim(),
                    TermsDays = Convert.ToInt32(remittanceAdvice.ttermsdays.Trim()),
                    CreatedAt = DateTime.Now,
                    Old = true
                };

                var details = from rxml in raLinqXml.remittanceadvice
                              from rxmlDetails in rxml.radetails
                              select rxmlDetails;

                foreach (var detail in details)
                {
                    RemittanceAdviceDetail detailRecord = new RemittanceAdviceDetail
                    {
                        DocumentDate = Convert.ToDateTime(detail.docdate.Trim()),
                        DocumentType = detail.doctype.Trim(),
                        SupplierDocumentNo = detail.supplierdocno.Trim(),
                        StoreCode = detail.storecode.Trim(),
                        StoreName = detail.storename.Trim(),
                        StoreEanCode = detail.storeeancode.Trim(),
                        DocumentAmountExcl = Convert.ToDecimal(detail.documentamountexcl.Trim()),
                        DocumentAmountIncl = Convert.ToDecimal(detail.documentamountincl.Trim()),
                        DiscountAmountExcl = Convert.ToDecimal(detail.discountamountexcl.Trim()),
                        DiscountAmountIncl = Convert.ToDecimal(detail.discountamountincl.Trim()),
                        VatAmountPayable = Convert.ToDecimal(detail.vatamountpayable.Trim()),
                        NetAmountIncl = Convert.ToDecimal(detail.netamountincl.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceDetails.Add(detailRecord);
                }

                var totalPayments = from rxml in raLinqXml.remittanceadvice
                                    from rxmlTotalPayment in rxml.totalforpayment
                                    select rxmlTotalPayment;

                foreach (var totalPayment in totalPayments)
                {
                    RemittanceAdviceTotalPayment totalPaymentRecord = new RemittanceAdviceTotalPayment
                    {
                        Description = totalPayment.totaldescription.Trim(),
                        DiscountAmountExcl = Convert.ToDecimal(totalPayment.totaldiscountamountexcl.Trim()),
                        DiscountAmountIncl = Convert.ToDecimal(totalPayment.totaldiscountamountincl.Trim()),
                        DocumentAmountExcl = Convert.ToDecimal(totalPayment.totaldocumentamountexcl.Trim()),
                        DocumentAmountIncl = Convert.ToDecimal(totalPayment.totaldocumentamountincl.Trim()),
                        VatAmountPayable = Convert.ToDecimal(totalPayment.totalvatamountpayable.Trim()),
                        NetAmountIncl = Convert.ToDecimal(totalPayment.totalnetamountincl.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceTotalPayments.Add(totalPaymentRecord);
                }

                var otherTotals = from rxml in raLinqXml.remittanceadvice
                                  from rxmlOtherTotal in rxml.othertotal
                                  select rxmlOtherTotal;

                foreach (var otherTotal in otherTotals)
                {
                    RemittanceAdviceOtherTotal otherTotalRecord = new RemittanceAdviceOtherTotal
                    {
                        Description = otherTotal.totaldescription.Trim(),
                        Amount = Convert.ToDecimal(otherTotal.amount.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceOtherTotals.Add(otherTotalRecord);
                }

                var taxInvoices = from rxmlTaxInvoice in raLinqXml.taxinvoice
                                  select rxmlTaxInvoice;

                var invoiceId = Guid.NewGuid();
                RemittanceAdviceTaxInvoice raTaxInvoice = new RemittanceAdviceTaxInvoice
                {
                    Id = invoiceId,
                };
                remittanceAdviceRecord.RemittanceAdviceTaxInvoices.Add(raTaxInvoice);

                using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required,
                        new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
                {
                    using (SparDSRemittanceAdvice db = new SparDSRemittanceAdvice())
                    {
                        var remittance = (from ra in db.RemittanceAdvices
                                          where ra.PaymentNumber == remittanceAdvice.paymentnumber && ra.DCEanCode == remittanceAdvice.dceancode
                                          select ra);

                        if (remittance == null)
                        {
                            db.RemittanceAdvices.InsertOnSubmit(remittanceAdviceRecord);

                            foreach (var taxInvoice in taxInvoices)
                            {
                                RemittanceAdviceOldTaxInvoice taxInvoiceOldRecord = new RemittanceAdviceOldTaxInvoice
                                {
                                    Id = invoiceId,
                                    InvoiceNumber = taxInvoice.invoicenumber.Trim(),
                                    CommissionDeductedDesciption = taxInvoice.totalcommissiondeducteddesc.Trim(),
                                    CommissionDeducted = Convert.ToDecimal(taxInvoice.totalcommissiondeducted.Trim()),
                                    VatOnCommissionDescription = taxInvoice.totalvatoncommissiondesc.Trim(),
                                    VatOnCommission = Convert.ToDecimal(taxInvoice.totalvatoncommission.Trim()),
                                    InclCommissionDeductedRADescription = taxInvoice.totalinclcommdeductedradesc.Trim(),
                                    InclCommissionDeductedRA = Convert.ToDecimal(taxInvoice.totalinclcommdeductedra.Trim()),
                                };
                                db.RemittanceAdviceOldTaxInvoices.InsertOnSubmit(taxInvoiceOldRecord);
                            }

                            db.SubmitChanges(ConflictMode.FailOnFirstConflict);
                        }
                        else if (remittance.Count() > 0)
                        {
                            var ra = remittance.FirstOrDefault();
                            if (ra.RemittanceAdviceTaxInvoices.FirstOrDefault() != null)
                            {
                                var raot =
                                   (from r in db.RemittanceAdviceOtherTaxes
                                    where r.RemittanceAdviceTaxInvocieId == ra.RemittanceAdviceTaxInvoices.FirstOrDefault().Id
                                    select r).DefaultIfEmpty();

                                foreach (var ti in raot)
                                    db.RemittanceAdviceOtherTaxes.DeleteOnSubmit(ti);

                                db.SubmitChanges(ConflictMode.FailOnFirstConflict);

                            }




                            if (ra.RemittanceAdviceTaxCreditNotes.FirstOrDefault() != null)
                            {
                                var x = (from r in db.RemittanceAdviceTaxCreditNotes
                                         where r.RemittanceAdviceId == ra.RemittanceAdviceTaxCreditNotes.FirstOrDefault().RemittanceAdviceId
                                         select r).DefaultIfEmpty();

                                foreach (var cn in x)
                                {
                                    var raOT = db.RemittanceAdviceOtherTaxes.Where(o => o.RemittanceAdviceTaxCreditNoteId == cn.Id).DefaultIfEmpty();
                                    foreach (var ot in raOT)
                                        db.RemittanceAdviceOtherTaxes.DeleteOnSubmit(ot);


                                    db.RemittanceAdviceTaxCreditNotes.DeleteOnSubmit(cn);
                                }

                                db.SubmitChanges(ConflictMode.FailOnFirstConflict);
                            }
                            db.SubmitChanges(ConflictMode.FailOnFirstConflict);

                            db.RemittanceAdvices.InsertOnSubmit(remittanceAdviceRecord);
                            db.SubmitChanges(ConflictMode.FailOnFirstConflict);
                        }
                        else
                        {
                            //ResponseResult is a Success as the transaction should not write to a DB if duplicated
                            returnResponse.ResponseResult = ResponseResult.Success;
                            returnResponse.ResponseMessage = "Transaction failed writing to DB";
                            string errorString = "Inserting Duplicate Record to Database; Payment Number: " + remittanceAdvice.paymentnumber.ToString() + " DCEanCode: " + remittanceAdvice.dceancode.ToString();
                            returnResponse.Trace.Add(errorString);
                            returnResponse.Trace.Add("Saving Remittance Advice to DB Failed");
                            return returnResponse;
                        }
                        ts.Complete();

                        returnResponse.ResponseMessage = "Transaction saved to DB";
                        returnResponse.Trace.Add("Saving Remittance Advice to DB Successful");
                        return returnResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = "Transaction failed writing to DB";
                returnResponse.Trace.Add(ex.Message);
                return returnResponse;
            }
        }

        public Response SaveRemittanceAdviceOldDC(string xml, Response returnResponse)
        {
            try
            {
                var raLinqXml = Spar.Data.XsdModels.RemittanceAdvice.OldDC.remittancedocument.Parse(xml);

                var raType = (from raXml in raLinqXml.remittanceadvice
                              select raXml.ratype).First();

                //This assumes that RAHeader will always be a single element in the XML
                var remittanceAdvice = (from rxml in raLinqXml.remittanceadvice
                                        from headers in rxml.raheader
                                        select headers).SingleOrDefault();
                RemittanceAdvice remittanceAdviceRecord = new RemittanceAdvice
                {
                    RAType = raType.ToString().Trim(),
                    DCEanCode = remittanceAdvice.dceancode.Trim(),
                    DCName = remittanceAdvice.dcname.Trim(),
                    DCPostalAddress1 = remittanceAdvice.dcpostaladdress1.Trim(),
                    DCPostalAddress2 = remittanceAdvice.dcpostaladdress2.Trim(),
                    DCPostalCode = remittanceAdvice.dcpostalcode.Trim(),
                    SupplierEanLocationCode = remittanceAdvice.suppliereanlocationcode.Trim(),
                    SupplierEanDispatchPoint = remittanceAdvice.suppliereandispatchpoint.Trim(),
                    DCVatRegNo = remittanceAdvice.dcvatregno.Trim(),
                    SupplierNumber = remittanceAdvice.suppliernumber.Trim(),
                    SupplierName = remittanceAdvice.suppliername.Trim(),
                    SupplierAddress1 = remittanceAdvice.supplieraddress1.Trim(),
                    SupplierAddress2 = remittanceAdvice.supplieraddress2.Trim(),
                    SupplierAddress3 = remittanceAdvice.supplieraddress3.Trim(),
                    SupplierPostalCode = remittanceAdvice.supplierpostalcode.Trim(),
                    SupplierVatRegNo = remittanceAdvice.suppliervatregno.Trim(),
                    PaymentNumber = remittanceAdvice.paymentnumber.Trim(),
                    ExtractedDate = Convert.ToDateTime(remittanceAdvice.extracteddate.Trim()),
                    RADate = Convert.ToDateTime(remittanceAdvice.radate.Trim()),
                    TermsCode = remittanceAdvice.termscode.Trim(),
                    TermsDays = Convert.ToInt32(remittanceAdvice.ttermsdays.Trim()),
                    CreatedAt = DateTime.Now,
                    Old = true
                };

                var details = from rxml in raLinqXml.remittanceadvice
                              from rxmlDetails in rxml.radetails
                              select rxmlDetails;

                foreach (var detail in details)
                {
                    RemittanceAdviceDetail detailRecord = new RemittanceAdviceDetail
                    {
                        DocumentDate = Convert.ToDateTime(detail.docdate.Trim()),
                        DocumentType = detail.doctype.Trim(),
                        SupplierDocumentNo = detail.supplierdocno.Trim(),
                        StoreCode = detail.storecode.Trim(),
                        StoreName = detail.storename.Trim(),
                        StoreEanCode = detail.storeeancode.Trim(),
                        DocumentAmountExcl = Convert.ToDecimal(detail.documentamountexcl.Trim()),
                        DocumentAmountIncl = Convert.ToDecimal(detail.documentamountincl.Trim()),
                        DiscountAmountExcl = Convert.ToDecimal(detail.discountamountexcl.Trim()),
                        DiscountAmountIncl = Convert.ToDecimal(detail.discountamountincl.Trim()),
                        VatAmountPayable = Convert.ToDecimal(detail.vatamountpayable.Trim()),
                        NetAmountIncl = Convert.ToDecimal(detail.netamountincl.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceDetails.Add(detailRecord);
                }

                var totalPayments = from rxml in raLinqXml.remittanceadvice
                                    from rxmlTotalPayment in rxml.totalforpayment
                                    select rxmlTotalPayment;

                foreach (var totalPayment in totalPayments)
                {
                    RemittanceAdviceTotalPayment totalPaymentRecord = new RemittanceAdviceTotalPayment
                    {
                        Description = totalPayment.totaldescription.Trim(),
                        DiscountAmountExcl = Convert.ToDecimal(totalPayment.totaldiscountamountexcl.Trim()),
                        DiscountAmountIncl = Convert.ToDecimal(totalPayment.totaldiscountamountincl.Trim()),
                        DocumentAmountExcl = Convert.ToDecimal(totalPayment.totaldocumentamountexcl.Trim()),
                        DocumentAmountIncl = Convert.ToDecimal(totalPayment.totaldocumentamountincl.Trim()),
                        VatAmountPayable = Convert.ToDecimal(totalPayment.totalvatamountpayable.Trim()),
                        NetAmountIncl = Convert.ToDecimal(totalPayment.totalnetamountincl.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceTotalPayments.Add(totalPaymentRecord);
                }

                var otherTotals = from rxml in raLinqXml.remittanceadvice
                                  from rxmlOtherTotal in rxml.othertotal
                                  select rxmlOtherTotal;

                foreach (var otherTotal in otherTotals)
                {
                    RemittanceAdviceOtherTotal otherTotalRecord = new RemittanceAdviceOtherTotal
                    {
                        Description = otherTotal.totaldescription.Trim(),
                        Amount = Convert.ToDecimal(otherTotal.amount.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceOtherTotals.Add(otherTotalRecord);
                }

                var taxInvoices = from rxmlTaxInvoice in raLinqXml.taxinvoice
                                  select rxmlTaxInvoice;

                var invoiceId = Guid.NewGuid();
                RemittanceAdviceTaxInvoice raTaxInvoice = new RemittanceAdviceTaxInvoice
                {
                    Id = invoiceId,
                };
                remittanceAdviceRecord.RemittanceAdviceTaxInvoices.Add(raTaxInvoice);
                using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required,
                        new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
                {
                    using (SparDSRemittanceAdvice db = new SparDSRemittanceAdvice())
                    {
                        var duplicate = (from ra in db.RemittanceAdvices
                                         where ra.PaymentNumber == remittanceAdvice.paymentnumber && ra.DCEanCode == remittanceAdvice.dceancode
                                         select ra).Count();
                        if (duplicate == 0)
                        {
                            db.RemittanceAdvices.InsertOnSubmit(remittanceAdviceRecord);

                            foreach (var taxInvoice in taxInvoices)
                            {
                                RemittanceAdviceOldTaxInvoice taxInvoiceOldRecord = new RemittanceAdviceOldTaxInvoice();
                                taxInvoiceOldRecord.Id = invoiceId;
                                taxInvoiceOldRecord.InvoiceNumber = taxInvoice.invoicenumber.Trim();
                                taxInvoiceOldRecord.CommissionDeductedDesciption = taxInvoice.totalallowancededucteddesc.Trim();
                                taxInvoiceOldRecord.CommissionDeducted = Convert.ToDecimal(taxInvoice.totalallowancededucted.Trim());
                                taxInvoiceOldRecord.VatOnCommissionDescription = taxInvoice.totalvatonallowancededucteddesc.Trim();
                                taxInvoiceOldRecord.VatOnCommission = Convert.ToDecimal(taxInvoice.totalvatonallowancededucted.Trim());
                                taxInvoiceOldRecord.InclCommissionDeductedRADescription = taxInvoice.totalinclallodeductedradesc.Trim();
                                taxInvoiceOldRecord.InclCommissionDeductedRA = Convert.ToDecimal(taxInvoice.totalinclallodeductedra.Trim());

                                db.RemittanceAdviceOldTaxInvoices.InsertOnSubmit(taxInvoiceOldRecord);
                            }

                            db.SubmitChanges(ConflictMode.FailOnFirstConflict);
                        }
                        else
                        {
                            //ResponseResult is a Success as the transaction should not write to a DB if duplicated
                            returnResponse.ResponseResult = ResponseResult.Success;
                            returnResponse.ResponseMessage = "Transaction failed writing to DB";
                            string errorString = "Inserting Duplicate Record to Database; Payment Number: " + remittanceAdvice.paymentnumber.ToString() + " DCEanCode: " + remittanceAdvice.dceancode.ToString();
                            returnResponse.Trace.Add(errorString);
                            returnResponse.Trace.Add("Saving Remittance Advice to DB Failed");
                            return returnResponse;
                        }
                        ts.Complete();

                        returnResponse.ResponseMessage = "Transaction saved to DB";
                        returnResponse.Trace.Add("Saving Remittance Advice to DB Successful");
                        return returnResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = "Transaction failed writing to DB";
                returnResponse.Trace.Add(ex.Message);
                return returnResponse;
            }
        }

        public Response SaveRemittanceAdviceOldDCCommission(string xml, Response returnResponse)
        {
            try
            {
                var raLinqXml = Spar.Data.XsdModels.RemittanceAdvice.OldDC.Commission.remittancedocument.Parse(xml);

                var raType = (from raXml in raLinqXml.remittanceadvice
                              select raXml.ratype).First();

                //This assumes that RAHeader will always be a single element in the XML
                var remittanceAdvice = (from rxml in raLinqXml.remittanceadvice
                                        from headers in rxml.raheader
                                        select headers).SingleOrDefault();
                RemittanceAdvice remittanceAdviceRecord = new RemittanceAdvice
                {
                    RAType = raType.ToString().Trim(),
                    DCEanCode = remittanceAdvice.dceancode.Trim(),
                    DCName = remittanceAdvice.dcname.Trim(),
                    DCPostalAddress1 = remittanceAdvice.dcpostaladdress1.Trim(),
                    DCPostalAddress2 = remittanceAdvice.dcpostaladdress2.Trim(),
                    DCPostalCode = remittanceAdvice.dcpostalcode.Trim(),
                    SupplierEanLocationCode = remittanceAdvice.suppliereanlocationcode.Trim(),
                    SupplierEanDispatchPoint = remittanceAdvice.suppliereandispatchpoint.Trim(),
                    DCVatRegNo = remittanceAdvice.dcvatregno.Trim(),
                    SupplierNumber = remittanceAdvice.suppliernumber.Trim(),
                    SupplierName = remittanceAdvice.suppliername.Trim(),
                    SupplierAddress1 = remittanceAdvice.supplieraddress1.Trim(),
                    SupplierAddress2 = remittanceAdvice.supplieraddress2.Trim(),
                    SupplierAddress3 = remittanceAdvice.supplieraddress3.Trim(),
                    SupplierPostalCode = remittanceAdvice.supplierpostalcode.Trim(),
                    SupplierVatRegNo = remittanceAdvice.suppliervatregno.Trim(),
                    PaymentNumber = remittanceAdvice.paymentnumber.Trim(),
                    ExtractedDate = Convert.ToDateTime(remittanceAdvice.extracteddate.Trim()),
                    RADate = Convert.ToDateTime(remittanceAdvice.radate.Trim()),
                    TermsCode = remittanceAdvice.termscode.Trim(),
                    TermsDays = Convert.ToInt32(remittanceAdvice.ttermsdays.Trim()),
                    CreatedAt = DateTime.Now,
                    Old = true
                };

                var details = from rxml in raLinqXml.remittanceadvice
                              from rxmlDetails in rxml.radetails
                              select rxmlDetails;

                foreach (var detail in details)
                {
                    RemittanceAdviceDetail detailRecord = new RemittanceAdviceDetail
                    {
                        DocumentDate = Convert.ToDateTime(detail.docdate.Trim()),
                        DocumentType = detail.doctype.Trim(),
                        SupplierDocumentNo = detail.supplierdocno.Trim(),
                        StoreCode = detail.storecode.Trim(),
                        StoreName = detail.storename.Trim(),
                        StoreEanCode = detail.storeeancode.Trim(),
                        DocumentAmountExcl = Convert.ToDecimal(detail.documentamountexcl.Trim()),
                        DocumentAmountIncl = Convert.ToDecimal(detail.documentamountincl.Trim()),
                        DiscountAmountExcl = Convert.ToDecimal(detail.discountamountexcl.Trim()),
                        DiscountAmountIncl = Convert.ToDecimal(detail.discountamountincl.Trim()),
                        VatAmountPayable = Convert.ToDecimal(detail.vatamountpayable.Trim()),
                        NetAmountIncl = Convert.ToDecimal(detail.netamountincl.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceDetails.Add(detailRecord);
                }

                var totalPayments = from rxml in raLinqXml.remittanceadvice
                                    from rxmlTotalPayment in rxml.totalforpayment
                                    select rxmlTotalPayment;

                foreach (var totalPayment in totalPayments)
                {
                    RemittanceAdviceTotalPayment totalPaymentRecord = new RemittanceAdviceTotalPayment
                    {
                        Description = totalPayment.totaldescription.Trim(),
                        DiscountAmountExcl = Convert.ToDecimal(totalPayment.totaldiscountamountexcl.Trim()),
                        DiscountAmountIncl = Convert.ToDecimal(totalPayment.totaldiscountamountincl.Trim()),
                        DocumentAmountExcl = Convert.ToDecimal(totalPayment.totaldocumentamountexcl.Trim()),
                        DocumentAmountIncl = Convert.ToDecimal(totalPayment.totaldocumentamountincl.Trim()),
                        VatAmountPayable = Convert.ToDecimal(totalPayment.totalvatamountpayable.Trim()),
                        NetAmountIncl = Convert.ToDecimal(totalPayment.totalnetamountincl.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceTotalPayments.Add(totalPaymentRecord);
                }

                var otherTotals = from rxml in raLinqXml.remittanceadvice
                                  from rxmlOtherTotal in rxml.othertotal
                                  select rxmlOtherTotal;

                foreach (var otherTotal in otherTotals)
                {
                    RemittanceAdviceOtherTotal otherTotalRecord = new RemittanceAdviceOtherTotal
                    {
                        Description = otherTotal.totaldescription.Trim(),
                        Amount = Convert.ToDecimal(otherTotal.amount.Trim())
                    };
                    remittanceAdviceRecord.RemittanceAdviceOtherTotals.Add(otherTotalRecord);
                }

                var taxInvoices = from rxmlTaxInvoice in raLinqXml.taxinvoice
                                  select rxmlTaxInvoice;

                var invoiceId = Guid.NewGuid();
                RemittanceAdviceTaxInvoice raTaxInvoice = new RemittanceAdviceTaxInvoice
                {
                    Id = invoiceId,
                };
                remittanceAdviceRecord.RemittanceAdviceTaxInvoices.Add(raTaxInvoice);

                using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required,
                        new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
                {
                    using (SparDSRemittanceAdvice db = new SparDSRemittanceAdvice())
                    {
                        var duplicate = (from ra in db.RemittanceAdvices
                                         where ra.PaymentNumber == remittanceAdvice.paymentnumber && ra.DCEanCode == remittanceAdvice.dceancode
                                         select ra).Count();
                        if (duplicate == 0)
                        {
                            db.RemittanceAdvices.InsertOnSubmit(remittanceAdviceRecord);

                            foreach (var taxInvoice in taxInvoices)
                            {
                                RemittanceAdviceOldTaxInvoice taxInvoiceOldRecord = new RemittanceAdviceOldTaxInvoice();
                                taxInvoiceOldRecord.Id = invoiceId;
                                taxInvoiceOldRecord.InvoiceNumber = taxInvoice.invoicenumber.Trim();
                                taxInvoiceOldRecord.CommissionDeductedDesciption = taxInvoice.totalcommissiondeducteddesc.Trim();
                                taxInvoiceOldRecord.CommissionDeducted = Convert.ToDecimal(taxInvoice.totalcommissiondeducted.Trim());
                                taxInvoiceOldRecord.VatOnCommissionDescription = taxInvoice.totalvatoncommissiondesc.Trim();
                                taxInvoiceOldRecord.VatOnCommission = Convert.ToDecimal(taxInvoice.totalvatoncommission.Trim());
                                taxInvoiceOldRecord.InclCommissionDeductedRADescription = taxInvoice.totalinclcommdeductedradesc.Trim();
                                taxInvoiceOldRecord.InclCommissionDeductedRA = Convert.ToDecimal(taxInvoice.totalinclcommdeductedra.Trim());

                                db.RemittanceAdviceOldTaxInvoices.InsertOnSubmit(taxInvoiceOldRecord);
                            }

                            db.SubmitChanges(ConflictMode.FailOnFirstConflict);
                        }
                        else
                        {
                            //ResponseResult is a Success as the transaction should not write to a DB if duplicated
                            returnResponse.ResponseResult = ResponseResult.Success;
                            returnResponse.ResponseMessage = "Transaction failed writing to DB";
                            string errorString = "Inserting Duplicate Record to Database; Payment Number: " + remittanceAdvice.paymentnumber.ToString() + " DCEanCode: " + remittanceAdvice.dceancode.ToString();
                            returnResponse.Trace.Add(errorString);
                            returnResponse.Trace.Add("Saving Remittance Advice to DB Failed");
                            return returnResponse;
                        }
                        ts.Complete();

                        returnResponse.ResponseMessage = "Transaction saved to DB";
                        returnResponse.Trace.Add("Saving Remittance Advice to DB Successful");
                        return returnResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                returnResponse.ResponseResult = ResponseResult.Failure;
                returnResponse.ResponseMessage = "Transaction failed writing to DB";
                returnResponse.Trace.Add(ex.Message);
                return returnResponse;
            }
        }
    }
}