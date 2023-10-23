using BizToDBIntegration.WcfContracts;
using Spar.Data.XsdModels.Recon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;
using System.Configuration;
using System.IO;


namespace Spar.Gateway.ReconImport
{
    internal sealed class ReconImportGateway
    {
        public Response Save(string xml,string UniqueIdentifier, Response response)
        {

            try
            {
                XElement xe = XElement.Parse(xml);
                ReconWeeklyImport reconXml = ReconWeeklyImport.Parse(xml);
 
                string reconreportdate = DateTime.Now.Year.ToString().Substring(0, 2) + reconXml.ReportDate.Substring(0, 2) + "/" + reconXml.ReportDate.Substring(2, 2) + "/" + reconXml.ReportDate.Substring(4, 2);
                DateTime reportDate = Convert.ToDateTime(reconreportdate);

                var stores = from store in reconXml.Store
                             select store;
                if (reconXml.EDIAccountingPoint == "" || reconXml.EDIAccountingPoint == null)
                {
                    response.ResponseResult = ResponseResult.Exception;
                    response.ResponseMessage = "File does not contain Ean code";
                    response.Trace.Add("Saving Recon to DB UnSuccessful");
                    return response;
                }
                else
                {
                    using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required,
                               new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted }))
                    {
                            using (SparDSReconImportDataContext db = new SparDSReconImportDataContext())
                            {
                                ReconReport report = new ReconReport
                            {
                                RRcEANNumber = reconXml.EDIAccountingPoint.Trim(),
                                RRcFileName = UniqueIdentifier.Trim(),
                                RRdDateReceived = DateTime.Now,
                                RRcFilePath = UniqueIdentifier.Trim(),
                                RRcVendorCode = reconXml.SupplierNumber.Trim(),
                                RRcEDIDispatch = reconXml.EDIDispatchPoint,
                                RRdReportDate = reportDate,
                                RRcDCEANCode = reconXml.DCEANCode

                            };

                            db.ReconReports.InsertOnSubmit(report);
                            db.SubmitChanges();

                            long reportId = report.RRID;

                            string variance = string.Empty;
                            foreach (var legend in reconXml.VarianceKeyLegend)
                            {
                                variance += legend.Key + ": " + legend.Description + " ";
                            }

                            ReconVariance reconVariance = new ReconVariance
                            {
                                RRID = Convert.ToInt32(reportId),
                                Descriptions = variance
                            };
                            db.ReconVariances.InsertOnSubmit(reconVariance);
                            db.SubmitChanges();

                            foreach (var store in stores)
                            {
                                try
                                {
                                    ReconHeader header = new ReconHeader
                                    {
                                        RRID = reportId,
                                        RHcStoreName = store.Name.Trim(),
                                        RHcAccountNumber = store.AccountNumber.Trim(),
                                        RHcAutoRecon = store.AutoRecon.Trim(),

                                    };

                                    db.ReconHeaders.InsertOnSubmit(header);
                                    db.SubmitChanges();

                                    long headerid = header.RHID;

                                    foreach (var line in store.Line)
                                    {
                                        try
                                        {
                                            DateTime? DocDate = null;
                                            if (line.LineDetails.First().DocDate != "")
                                            {
                                                DocDate = Convert.ToDateTime(DateTime.Now.Year.ToString().Substring(0, 2) + line.LineDetails.First().DocDate.Substring(0, 2) + "/" + line.LineDetails.First().DocDate.Substring(2, 2) + "/" + line.LineDetails.First().DocDate.Substring(4, 2));
                                            }

                                            DateTime? payDueDate = null;
                                            if (line.LineDetails.First().PayDueDate != "")
                                            {
                                                payDueDate = Convert.ToDateTime(DateTime.Now.Year.ToString().Substring(0, 2) + line.LineDetails.First().PayDueDate.Substring(0, 2) + "/" + line.LineDetails.First().PayDueDate.Substring(2, 2) + "/" + line.LineDetails.First().PayDueDate.Substring(4, 2));
                                            }

                                            DateTime? refDocDate = null;
                                            string d = line.LineDetails.First().RefDocDate;
                                            if (line.LineDetails.First().RefDocDate != "")
                                            {
                                                refDocDate = Convert.ToDateTime(DateTime.Now.Year.ToString().Substring(0, 2) + line.LineDetails.First().RefDocDate.Substring(0, 2) + "/" + line.LineDetails.First().RefDocDate.Substring(2, 2) + "/" + line.LineDetails.First().RefDocDate.Substring(4, 2));
                                            }
                                            ReconLine reconline = new ReconLine
                                            {
                                                RHID = headerid,
                                                RRcEANNumber = reconXml.EDIAccountingPoint.Trim(),
                                                RLcLineType = line.LineType.Trim(),
                                                RLcDocType = (line.LineDetails.First().DocType != "") ? line.LineDetails.First().DocType.Trim() : "",
                                                RLcInvNumber = (line.LineDetails.First().InvNo != "") ? line.LineDetails.First().InvNo.Trim() : "",
                                                RLcCrnNumber = (line.LineDetails.First().CrnNo != "") ? line.LineDetails.First().CrnNo.Trim() : "",
                                                RLcGrvNumber = (line.LineDetails.First().GrvNo != "") ? line.LineDetails.First().GrvNo.Trim() : "",
                                                RLcClmNumber = (line.LineDetails.First().ClmNo1 != "") ? line.LineDetails.First().ClmNo1.Trim() : "",
                                                RLcRefDoc = (line.LineDetails.First().RefDocNo != "") ? line.LineDetails.First().RefDocNo.Trim() : "",
                                                RLdDocDate = DocDate,
                                                RLdPayDueDate = payDueDate,
                                                RLdRefDocDate = refDocDate,
                                                RLcVarianceKey = line.LineDetails.First().VarianceKey,
                                                RLmAmountIncluded = Convert.ToDecimal(line.LineAmount[0].Amount.Trim()),
                                                RLmClaimAmount = Convert.ToDecimal(line.LineAmount[1].Amount.Trim()),
                                                RLmNetAmount = Convert.ToDecimal(line.LineAmount[2].Amount.Trim()),
                                                RLmCreditAmount = 0

                                            };

                                            db.ReconLines.InsertOnSubmit(reconline);
                                            db.SubmitChanges();
                                        }
                                        catch (Exception ex)
                                        {
                                            //throw new Exception(ex.Message + line.LineDetails.First().PayDueDate);
                                            response.ResponseResult = ResponseResult.Exception;
                                            response.ResponseMessage = ex.Message + line.LineDetails.First().PayDueDate;
                                            response.Trace.Add("Saving Recon to DB UnSuccessful");
                                            return response;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    //throw new Exception(ex.Message + store.AccountNumber);
                                    response.ResponseResult = ResponseResult.Exception;
                                    response.ResponseMessage = ex.Message + store.AccountNumber;
                                    response.Trace.Add("Saving Recon to DB UnSuccessful");
                                    return response;
                                }


                            }

                        }
                        string finame = ConfigurationManager.AppSettings["OutputPath"].ToString() + UniqueIdentifier;
                        xe.Save(finame);

                        var local = Path.Combine(@ConfigurationManager.AppSettings["OutputPath"].ToString(), UniqueIdentifier);
                        var remote = Path.Combine(@ConfigurationManager.AppSettings["ArchiveTo"].ToString(), UniqueIdentifier);

                        File.Copy(local,remote);
                        File.Delete(local);
                        ts.Complete();

                        response.ResponseMessage = "Transaction saved to DB";
                        response.Trace.Add("Saving Recon to DB Successful");
                        return response;
                    }
                }

            }
            catch (Exception ex)
            {
                //throw new Exception(ex.Message);
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }

        }
    }
}
