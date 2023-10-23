using System;
using System.Transactions;
using System.Linq;
using BizToDBIntegration.WcfContracts;
using System.Xml.Linq;
using DocumentTracking.Gateway;
using System.Data.Linq;

namespace DocumentTracking.Manager
{
    internal class PTHPRPGateway
    {
        public PTHPRPGateway()
        {
        }

        internal Response Save(string data, Response response)
        {
            using (var ts = new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (DocumentTrackingModelDataContext db = new DocumentTrackingModelDataContext())
                {
                    try
                    {
                        var linqPTHPRP = Data.XsdModels.PTHPRP.REPORTS.Parse(data);
                        var unb = linqPTHPRP.PTHPRP.UNB;
                        var documentType = "PTHRPT";

                        var existingTrackingDocument = (from td in db.TrackingDocuments
                                                        join tds in db.TrackingDocumentSystems on td.System_Id equals tds.Id
                                                        where td.DocumentType == documentType
                                                            && td.DocumentIdentifier == unb.InterchangeControlReference
                                                            && td.SourceTradingPartnerEan == unb.InterchangeSender.SendersIdentification
                                                            && td.DestinationTradingPartnerEan == unb.InterchangeRecipient.ReceiversIdentification
                                                            && td.DocumentDeliveryPoint == unb.InterchangeRecipient.ReceiversIdentification
                                                        select new { tds, td }).FirstOrDefault();

                        if (existingTrackingDocument != null)
                        {
                            var trackingDocumentSystem = (db.TrackingDocumentSystems.Where(
                                    sys => sys.Id == existingTrackingDocument.tds.Id)
                                ).FirstOrDefault();

                            trackingDocumentSystem.Timestamp = DateTime.Now;
                            var trackingDocumentDetail = new TrackingDocumentDetail
                            {
                                DocumentType = "PTHRPT",
                                DocumentFormat = "XML",
                                DocumentTimestamp = unb.TransmissionDateTime.Date,
                                SourceTradingPartnerEan = unb.InterchangeSender.SendersIdentification,
                                DestinationTradingPartnerEan = unb.InterchangeRecipient.ReceiversIdentification,
                                TrackingDocumentId = existingTrackingDocument.td.Id
                            };


                            db.TrackingDocumentDetails.InsertOnSubmit(trackingDocumentDetail);
                            db.SubmitChanges(ConflictMode.ContinueOnConflict);
                        }
                        else
                        {

                            XElement customData = new XElement("CustomData");
                            customData.Add(new XElement("Custom", new XElement("Column", "Interchange"), new XElement("Value", unb.InterchangeControlReference.ToString())));
                            customData.Add(new XElement("Custom", new XElement("Column", "Number of Reports ="), new XElement("Value", unb.UNH.Count)));


                            var trackingDocument = new TrackingDocument()
                            {
                                DocumentType = "PTHRPT",
                                DocumentFormat = "XML",
                                DocumentIdentifier = unb.InterchangeControlReference,
                                DocumentTimestamp = unb.TransmissionDateTime.Date,
                                DestinationInterchangeNumber = unb.InterchangeControlReference,
                                DocumentDeliveryPoint = unb.InterchangeRecipient.ReceiversIdentification,
                                DestinationTradingPartnerEan = unb.InterchangeRecipient.ReceiversIdentification,
                                SourceInterchangeNumber = unb.InterchangeControlReference,
                                SourceTradingPartnerEan = unb.InterchangeSender.SendersIdentification,
                                CustomData = customData.ToString(),
                                TrackingDocumentSystem = new TrackingDocumentSystem
                                {
                                    Name = "TRANSFORMATION",
                                    GroupId = "1234567890",
                                    Timestamp = unb.TransmissionDateTime.Date,
                                    FileInputPath = string.Empty,
                                    FileOutputPath = string.Empty,
                                    Status = "SUCCESS",
                                    StatusMessage = string.Empty
                                },
                            };



                            foreach (var unh in unb.UNH)
                            {
                                var reference = unh.BGM.Reference;

                                if (reference != null)
                                {
                                    trackingDocument.TrackingDocumentReferences.Add(new TrackingDocumentReference
                                    {
                                        DocumentType = reference.ReferenceQualifier ?? string.Empty,
                                        DocumentIdentifier = reference.ReferenceNumber ?? string.Empty,
                                        DocumentTimestamp = unb.TransmissionDateTime.Date
                                    });
                                }

                            }



                            trackingDocument.TrackingDocumentDetails.Add(new TrackingDocumentDetail
                            {
                                DocumentType = "PTHRPT",
                                DocumentFormat = "XML",
                                DocumentTimestamp = unb.TransmissionDateTime.Date,
                                SourceTradingPartnerEan = unb.InterchangeSender.SendersIdentification.ToString(),
                                DestinationTradingPartnerEan = unb.InterchangeRecipient.ReceiversIdentification.ToString()
                            });


                            db.TrackingDocuments.InsertOnSubmit(trackingDocument);
                            db.SubmitChanges();
                        }

                        ts.Complete();
                        response.ResponseResult = ResponseResult.Success;
                        response.ResponseMessage = "PTHPRP saved to DB";
                        response.Trace.Add("PTHPRP added");
                    }
                    catch (Exception e)
                    {
                        response.ResponseResult = ResponseResult.Exception;
                        response.ResponseMessage = "PTHPRP failed saving to DB";
                        response.Trace.Add(e.Message);
                        return response;
                    }

                    return response;
                }
            }
        }
    }
}