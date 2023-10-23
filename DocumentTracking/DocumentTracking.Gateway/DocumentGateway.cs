using System;
using System.Diagnostics;
using System.Transactions;
using System.Linq;
using BizToDBIntegration.WcfContracts;
using System.Data.Linq;
using System.Xml.Linq;
using DocumentTracking.Models;
using System.Collections.Generic;

namespace DocumentTracking.Gateway
{
    internal sealed class DocumentGateway
    {
        public Response Save(string xml, Response response)
        {

            try
            {
                XElement xe = XElement.Parse(xml);
                string custom = xe.Descendants("CustomData").Single().ToString();
                DocTrack orderLinqXml = DocTrack.Parse(xml);
                var document = (from track in orderLinqXml.Track
                                from doc in track.Document
                                select doc).FirstOrDefault();
                var documentReferences = (from track in orderLinqXml.Track
                                          from doc in track.Document
                                          from reference in doc.References
                                          select reference);
                var trackSystem = (from track in orderLinqXml.Track
                                   from system in track.System
                                   select system).FirstOrDefault();

                using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required,
                                  new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted, Timeout = new TimeSpan(0, 5, 0) }))
                {

                    using (DocumentTrackingModelDataContext db = new DocumentTrackingModelDataContext())
                    {
                        try
                        {
                            var SourceEan = (from source in document.Source
                                             select source.TradingPartnerEan).FirstOrDefault();
                            var DestinationEan = (from destination in document.Destination
                                                  select destination.TradingPartnerEan).FirstOrDefault();

                            var trackdoc = (from doc in db.TrackingDocuments
                                            join docsys in db.TrackingDocumentSystems on doc.System_Id equals docsys.Id
                                            where doc.DocumentType == document.Type.Trim()
                                                && doc.DocumentIdentifier == document.Number.Trim()
                                                && doc.SourceTradingPartnerEan == SourceEan
                                                && doc.DestinationTradingPartnerEan == DestinationEan
                                                && doc.DocumentDeliveryPoint == document.DeliveryPoint.Trim()
                                            select new { doc, docsys }).FirstOrDefault();
                            if (trackdoc != null)
                            {
                                TrackingDocumentSystem docsystem = (db.TrackingDocumentSystems.Where(sys => sys.Id == trackdoc.docsys.Id)).FirstOrDefault();

                                docsystem.Timestamp = DateTime.Now; // Convert.ToDateTime(document.Date.Trim())//Convert.ToDateTime(trackSystem.Timestamp.Trim());

                                TrackingDocumentDetail detail = new TrackingDocumentDetail
                                {
                                    DocumentType = document.Type.Trim(),
                                    DocumentFormat = document.Format.Trim(),
                                    DocumentTimestamp = Convert.ToDateTime(trackSystem.Timestamp.Trim()),
                                    SourceTradingPartnerEan = (from source in document.Source
                                                               select source.TradingPartnerEan).FirstOrDefault(),
                                    DestinationTradingPartnerEan = (from destination in document.Destination
                                                                    select destination.TradingPartnerEan).FirstOrDefault(),
                                    TrackingDocumentId = trackdoc.doc.Id
                                };
                                db.TrackingDocumentDetails.InsertOnSubmit(detail);
                                try
                                {
                                    db.SubmitChanges(ConflictMode.ContinueOnConflict);
                                }
                                catch (ChangeConflictException ex)
                                {
                                    //foreach (ObjectChangeConflict occ in db.ChangeConflicts)
                                    //{
                                    //    //No database values are merged into current.
                                    //    occ.Resolve(RefreshMode.KeepChanges);
                                    //}
                                }
                                response.ResponseMessage = "Transaction saved to DB";
                                response.Trace.Add("Saving Document to DB Successful");
                                return response;
                            }
                            else
                            {
                                try
                                {
                                    TrackingDocument TrackDocument = new TrackingDocument
                                    {

                                        DocumentType = document.Type.Trim(),
                                        DocumentFormat = document.Format.Trim(),
                                        DocumentIdentifier = document.Number.Trim(),
                                        DocumentTimestamp = DateTime.Now,//Convert.ToDateTime(document.Date.Trim()),
                                        DocumentDeliveryPoint = document.DeliveryPoint.Trim(),
                                        SourceInterchangeNumber = (from source in document.Source
                                                                   select source.InterchangeNumber).FirstOrDefault(),
                                        SourceTradingPartnerEan = (from source in document.Source
                                                                   select source.TradingPartnerEan).FirstOrDefault(),
                                        DestinationInterchangeNumber = (from destination in document.Destination
                                                                        select destination.InterchangeNumber).FirstOrDefault(),
                                        DestinationTradingPartnerEan = (from destination in document.Destination
                                                                        select destination.TradingPartnerEan).FirstOrDefault(),
                                        TrackingDocumentSystem = new TrackingDocumentSystem
                                        {

                                            Name = trackSystem.SystemName.ToString().Trim(),
                                            GroupId = trackSystem.GeneratedId.Trim(),
                                            Timestamp = Convert.ToDateTime(trackSystem.Timestamp.Trim()),
                                            FileInputPath = (from input in trackSystem.File
                                                             select input.InputPath).FirstOrDefault(),
                                            FileOutputPath = (from ouput in trackSystem.File
                                                              select ouput.OutputPath).FirstOrDefault(),
                                            Status = trackSystem.Status.Trim(),
                                            StatusMessage = trackSystem.StatusMessage.Trim()

                                        },
                                        CustomData = custom
                                    };

                                    TrackDocument.TrackingDocumentDetails.Add(new TrackingDocumentDetail()
                                    {
                                        DocumentType = document.Type.Trim(),
                                        DocumentFormat = document.Format.Trim(),
                                        DocumentTimestamp = DateTime.Now,//Convert.ToDateTime(trackSystem.Timestamp.Trim()),
                                        SourceTradingPartnerEan = (from source in document.Source
                                                                   select source.TradingPartnerEan).FirstOrDefault(),
                                        DestinationTradingPartnerEan = (from destination in document.Destination
                                                                        select destination.TradingPartnerEan).FirstOrDefault(),
                                    });


                                    foreach (var documentReference in documentReferences)
                                    {
                                        TrackingDocumentReference trackDocumentReference = new TrackingDocumentReference
                                        {

                                            DocumentType = (from reference in documentReference.Reference
                                                            select reference.DocumentType).FirstOrDefault(),
                                            DocumentIdentifier = (from reference in documentReference.Reference
                                                                  select reference.DocumentNumber).FirstOrDefault(),
                                            DocumentTimestamp = Convert.ToDateTime((from reference in documentReference.Reference
                                                                                    select reference.Date).FirstOrDefault())
                                        };
                                        TrackDocument.TrackingDocumentReferences.Add(trackDocumentReference);
                                    }


                                    db.TrackingDocuments.InsertOnSubmit(TrackDocument);
                                    db.SubmitChanges();

                                }
                                catch (Exception e)
                                {
                                    response.ResponseResult = ResponseResult.Exception;
                                    response.ResponseMessage = "Transaction failed writing to DB";
                                    response.Trace.Add(e.Message);
                                    return response;
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            response.ResponseResult = ResponseResult.Exception;
                            response.ResponseMessage = "Transaction failed writing to DB";
                            response.Trace.Add(ex.Message);
                            return response;
                        }

                        ts.Complete();

                        response.ResponseMessage = "Transaction saved to DB";
                        response.Trace.Add("Saving Document to DB Successful");
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }

        }

        internal Response SaveGRN(string data, Response response)
        {
            try
            {
                bool success = true;
                response.ResponseMessage = "";

                XElement xe = XElement.Parse(data);

                var fileInformation = xe.Descendants("FileInformation").FirstOrDefault();
                var fileType = fileInformation.Element("FileType").Value;
                var unb = xe.Descendants("UNB").FirstOrDefault();
                var unh = unb.Descendants("UNH").FirstOrDefault();

                var document = new Document
                {
                    Type = "GRN/GRV",
                    Sender = unb.Element("InterchangeSender").Element("SenderIdentification").Value,
                    Receiver = unb.Element("InterchangeRecipient").Element("ReceiversIdentification").Value,
                    InterchangeNumber = string.Empty,
                    SystemDate = DateTime.Now,
                    DeliveryPoint = unh.Element("CLO").Element("CustomerDeliveryInvoicePoint").Value,
                    DeliveryPointName = unh.Element("CLO").Element("CustomerDeliveryPointName").Value,
                    Date = Convert.ToDateTime(unh.Element("DEL").Element("DeliveryNote").Element("Date").Value),
                    Number = unh.Element("DEL").Element("DeliveryNote").Element("Number").Value,
                    Format = fileType,

                };

                List<Reference> references = new List<Reference>
                {
                    new Reference()
                    {
                        Type = "ORDER",
                        Number = unh.Element("ORF").Element("OrderNumberAndDate").Element("CustomerOrderNumber").Value,
                        Date = DateTime.Now
                    }
                };

                document.References = references;

                response = InsertIntoDocumentTracking(document, response);
                if (response.ResponseResult == ResponseResult.Exception)
                {

                    success = false;
                    response.ResponseMessage = response.ResponseMessage + document.Number + " processing unsuccessfull";
                }


                if (!success)
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Transaction successfull with errors on some grn's saving to DB";
                    response.Trace.Add("Successfull with errors on some grn's");
                    return response;
                }

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "Transaction Successfull writing to DB";
                response.Trace.Add("Successfull");

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }

        }

        internal Response SaveClaim(string data, Response response)
        {
            try
            {
                bool success = true;
                response.ResponseMessage = "";

                XElement xe = XElement.Parse(data);

                var claim = xe.Descendants("Claim").FirstOrDefault();

                var document = new Document
                {
                    Type = "CLAIM",
                    Sender = claim.Element("Header").Element("Sender").Value,
                    Receiver = claim.Element("Header").Element("Receiver").Value,
                    InterchangeNumber = claim.Element("Header").Element("InterchangeNo").Value,
                    SystemDate = Convert.ToDateTime(claim.Element("Header").Element("Date").Value),
                    DeliveryPoint = claim.Element("CLO").Element("CustomerDeliveryPoint").Value,
                    Number = claim.Element("REF").Element("ClaimNum").Element("ClaimNumber").Value,
                    Date = Convert.ToDateTime(claim.Element("REF").Element("ClaimNum").Element("ClaimDate").Value),
                    Format = string.Empty,

                };

                List<Reference> references = new List<Reference>();

                var claimReference = claim.Element("CRF").Element("ClaimReference");

                if (claimReference.Element("InvNum") != null)
                    references.Add(new Reference()
                    {
                        Type = "INVOICE",
                        Number = claimReference.Element("InvNum").Value,
                        Date = Convert.ToDateTime(claimReference.Element("InvDate").Value),
                    });

                if (claimReference.Element("OrderNum") != null)
                    references.Add(new Reference()
                    {
                        Type = "ORDER",
                        Number = claimReference.Element("OrderNum").Value,
                        Date = Convert.ToDateTime(claimReference.Element("OrderDate").Value),
                    });

                document.References = references;

                response = InsertIntoDocumentTracking(document, response);
                if (response.ResponseResult == ResponseResult.Exception)
                {
                    success = false;
                    response.ResponseMessage = response.ResponseMessage + document.Number + " processing unsuccessfull";
                }


                if (!success)
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Transaction successfull with errors on some claims saving to DB";
                    response.Trace.Add("Successfull with errors on some claims");
                    return response;
                }

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "Transaction successfull writing to DB";
                response.Trace.Add("Successfull");

                return response;
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }

        internal Response SaveCreditNote(string data, Response response)
        {
            try
            {
                bool success = true;
                response.ResponseMessage = "";
                XElement xe = XElement.Parse(data);
                var fileInfo = xe.Descendants("FileInformation").FirstOrDefault();
                var format = fileInfo.Element("FileType").Value;
                var messageDetail = xe.Descendants("Message_Detail").FirstOrDefault();

                var header = xe.Descendants("Interchange").Descendants("Header").FirstOrDefault();

                //var document = new Document
                //{
                var Type = "CREDIT";
                  var  Sender = header.Element("Sender_Identification").Value;
             var    Receiver = header.Element("Recipient_Identification").Value;
               var InterchangeNumber = header.Element("Interchange_Control_Reference").Value;
             var   SystemDate = Convert.ToDateTime(header.Element("DATE_AND_TIME_OF_TRANSMISSION").Value);
             var   DeliveryPoint = messageDetail.Element("CLO").Element("Customer_Delivery_Invoice_Point").Value;
             var   DeliveryPointName = string.IsNullOrEmpty(messageDetail.Element("CLO").Element("Customer_Delivery_Point_Name").Value) ? string.Empty : messageDetail.Element("CLO").Element("Customer_Delivery_Point_Name").Value;
              var      Date = Convert.ToDateTime(messageDetail.Element("REF").Element("REFC010_REFERENCES").Element("Date").Value);
             var   Number = messageDetail.Element("REF").Element("REFC010_REFERENCES").Element("Reference_number").Value;
                var Format = format;
               // };

                var trailer = messageDetail.Element("BTT");
                XElement custom = new XElement("CustomData");
                custom.Add(new XElement("Custom", new XElement("Column", "Total Amount Excl Vat"), new XElement("Value", (trailer.Element("Batch_Total_Amount_Excluding_Vat").Value ?? "0.00"))));
                //custom.Add(new XElement("Custom", new XElement("Column", "Store Name"), new XElement("Value", document.DeliveryPointName)));
                //document.CustomXML = custom.ToString();

                var originalReferences = messageDetail.Element("DCF").Elements("DOCUMENT_REFERENCE");

                List<Reference> references = new List<Reference>();

                foreach (var reference in originalReferences)
                {
                    references.Add(new Reference()
                    {
                        Type = reference.Element("Document_type").Value,
                        Number = reference.Element("Reference_number") == null ? string.Empty : reference.Element("Reference_number").Value,
                        Date = reference.Element("Date") == null ? DateTime.Now : Convert.ToDateTime(reference.Element("Date").Value),
                    });
                }


                //document.References = references;

                //response = InsertIntoDocumentTracking(document, response);
                //if (response.ResponseResult == ResponseResult.Exception)
                //{

                //    success = false;
                //    response.ResponseMessage = response.ResponseMessage + document.Number + " Processing Unsuccessfull";
                //}


                if (!success)
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Transaction Successfull with errors on some orders writing to DB";
                    response.Trace.Add("Successfull with errors on some credit notes");
                    return response;
                }

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "Transaction Successfull writing to DB";
                response.Trace.Add("Successfull");
                return response;

            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }

        public Response SaveOrder(string xml, Response response)
        {

            try
            {
                bool success = true;
                response.ResponseMessage = "";
                string format = string.Empty;
                XElement xe = XElement.Parse(xml);
                var fileInfo = xe.Descendants("FileInformation").FirstOrDefault();
                format = fileInfo.Element("FileType").Value;
                var orders = xe.Descendants("Order");
                foreach (var ord in orders)
                {
                    Document document = new Document
                    {
                        Type = "ORDERS"
                    };
                    var header = ord.Descendants("Header").FirstOrDefault();
                    document.Sender = header.Element("Sender").Value;
                    document.Receiver = header.Element("Receiver").Value;
                    document.InterchangeNumber = header.Element("InterchangeNo").Value;
                    document.SystemDate = Convert.ToDateTime(header.Element("Date").Value);
                    var deliveryDetails = header.Descendants("CustomerLocation").FirstOrDefault();
                    document.DeliveryPoint = deliveryDetails.Element("CustomerDeliveryPoint").Value;
                    document.DeliveryPointName = (deliveryDetails.Element("CustomerDeliveryPointName") != null) ? deliveryDetails.Element("CustomerDeliveryPointName").Value : "";
                    var orderDetails = header.Descendants("OrderDetails").FirstOrDefault();
                    document.Date = Convert.ToDateTime(orderDetails.Element("OrderDate").Value);
                    document.Number = orderDetails.Element("CustomerOrderNumber").Value;
                    document.Format = format;
                    XElement custom = new XElement("CustomData");
                    var trailer = header.Descendants("OrderTrailer").FirstOrDefault();

                    var transactionCode = orderDetails.Descendants("OrderDetailsTransactionCode")
                        .FirstOrDefault()
                        .Element("TransactionCode")
                        .Value;

                    if (transactionCode == "U")
                        document.Type = "CANCEL";

                    if (trailer != null)
                        custom.Add(new XElement("Custom", new XElement("Column", "Total Amount Excl Vat"), new XElement("Value", (trailer.Element("OrderTotalInclDiscExclVat").Value ?? "0.00"))));
                    custom.Add(new XElement("Custom", new XElement("Column", "Store Name"), new XElement("Value", document.DeliveryPointName)));
                    document.CustomXML = custom.ToString();

                    response = InsertIntoDocumentTracking(document, response);
                    if (response.ResponseResult == ResponseResult.Exception)
                    {

                        success = false;
                        response.ResponseMessage = response.ResponseMessage + document.Number + " Processing Unsuccessfull";
                    }

                }

                if (!success)
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Transaction Successfull with errors on some orders writing to DB";
                    response.Trace.Add("Successfull with errors on some orders");
                    return response;
                }

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "Transaction Successfull writing to DB";
                response.Trace.Add("Successfull");
                return response;

            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }

        }

        public Response SaveInvoice(string xml, Response response)
        {

            try
            {
                bool success = true;
                response.ResponseMessage = "";
                string format = string.Empty;
                XElement xe = XElement.Parse(xml);
                var fileInfo = xe.Descendants("FileInformation").FirstOrDefault();
                format = fileInfo.Element("FileType").Value;
                var invoices = xe.Descendants("TAXINV");
                foreach (var invoice in invoices)
                {
                    Document document = new Document
                    {
                        Type = "TAXINV"
                    };

                    if (format == "ASN" || format == "INVOIC")
                        document.Type = format;

                    var unb = invoice.Descendants("UNB").FirstOrDefault();
                    var sender = unb.Descendants("INTERCHANGESENDER").FirstOrDefault();
                    document.Sender = sender.Element("SenderIdentification").Value;
                    var receiver = unb.Descendants("INTERCHANGERECIPIENT").FirstOrDefault();
                    document.Receiver = receiver.Element("RecipientIdentification").Value;
                    document.InterchangeNumber = unb.Element("InterchangeControlReference").Value;
                    var createddate = unb.Descendants("CreationDateTime").FirstOrDefault();
                    document.SystemDate = Convert.ToDateTime(createddate.Element("Date").Value);
                    var deliveryDetails = invoice.Descendants("CLO").FirstOrDefault();
                    document.DeliveryPoint = deliveryDetails.Element("CustomerDeliveryInvoicePoint").Value;
                    document.DeliveryPointName = (deliveryDetails.Element("CustomerDeliveryPointName") != null) ? deliveryDetails.Element("CustomerDeliveryPointName").Value : "";
                    var ire = invoice.Descendants("IRE").FirstOrDefault();
                    var invoicedetails = ire.Descendants("InvoiceReferences").FirstOrDefault();
                    document.Date = Convert.ToDateTime(invoicedetails.Element("Date").Value);
                    document.Number = invoicedetails.Element("Number").Value;
                    document.Format = format;

                    XElement custom = new XElement("CustomData");
                    var trailer = invoice.Descendants("BTT").FirstOrDefault();
                    if (trailer != null)
                        custom.Add(new XElement("Custom", new XElement("Column", "Total Amount Excl Vat"), new XElement("Value", (trailer.Element("BatchTotalExclVat").Value ?? "0.00"))));
                    custom.Add(new XElement("Custom", new XElement("Column", "Store Name"), new XElement("Value", document.DeliveryPointName)));
                    document.CustomXML = custom.ToString();
                    var order = invoice.Descendants("ODD").FirstOrDefault().Descendants("OrderNumberAndDate").FirstOrDefault();
                    //var order = odd.Descendants("OrderNumberAndDate").FirstOrDefault();

                    List<Reference> references = new List<Reference>
                    {
                        new Reference()
                        {
                            Type = "ORDER",
                            Number = order.Element("CustomerOrderNumber").Value,
                            Date = Convert.ToDateTime(order.Element("DateOrderPlacedByCustomer").Value),
                        }
                    };

                    document.References = references;

                    response = InsertIntoDocumentTracking(document, response);
                    if (response.ResponseResult == ResponseResult.Exception)
                    {

                        success = false;
                        response.ResponseMessage = response.ResponseMessage + document.Number + " Processing Unsuccessfull";
                    }

                }

                if (!success)
                {
                    response.ResponseResult = ResponseResult.Failure;
                    response.ResponseMessage = "Transaction Successfull with errors on some orders writing to DB";
                    response.Trace.Add("Successfull with errors on some orders");
                    return response;
                }

                response.ResponseResult = ResponseResult.Success;
                response.ResponseMessage = "Transaction Successfull writing to DB";
                response.Trace.Add("Successfull");
                return response;

            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }

        }

        public Response SaveDeliveryNote(string data, Response response)
        {
            using (var ts = new TransactionScope(TransactionScopeOption.Required,
                 new TransactionOptions { IsolationLevel = IsolationLevel.ReadUncommitted }))
            {
                using (DocumentTrackingModelDataContext db = new DocumentTrackingModelDataContext())
                {
                    try
                    {
                        var linqDeliveryNote = Data.XsdModels.DeliveryNote.Ordresp.Parse(data);
                        var documentType = "ORDRES";
                        var header = linqDeliveryNote.Ordrsp.Header;

                        var existingTrackingDocument = (from td in db.TrackingDocuments
                                                        join tds in db.TrackingDocumentSystems on td.System_Id equals tds.Id
                                                        where td.DocumentType == documentType
                                                            && td.DocumentIdentifier == header.InterchangeNo
                                                            && td.SourceTradingPartnerEan == header.Sender
                                                            && td.DestinationTradingPartnerEan == header.Receiver
                                                            && td.DocumentDeliveryPoint == header.MessageHeader.CustomerLocation.CustomerDeliveryPoint
                                                        select new { tds, td }).FirstOrDefault();

                        if (existingTrackingDocument != null)
                        {
                            var trackingDocumentSystem = (db.TrackingDocumentSystems.Where(
                                   sys => sys.Id == existingTrackingDocument.tds.Id)
                               ).FirstOrDefault();

                            trackingDocumentSystem.Timestamp = DateTime.Now;
                            var trackingDocumentDetail = new TrackingDocumentDetail
                            {
                                DocumentType = documentType,
                                DocumentFormat = "XML",
                                DocumentTimestamp = header.Date,
                                SourceTradingPartnerEan = header.Sender,
                                DestinationTradingPartnerEan = header.Receiver,
                                TrackingDocumentId = existingTrackingDocument.td.Id
                            };


                            db.TrackingDocumentDetails.InsertOnSubmit(trackingDocumentDetail);
                            db.SubmitChanges(ConflictMode.ContinueOnConflict);
                        }
                        else
                        {
                            XElement customData = new XElement("CustomData");
                            customData.Add(new XElement("Custom", new XElement("Column", "Interchange"), new XElement("Value", header.InterchangeNo)));
                            customData.Add(new XElement("Custom", new XElement("Column", "Number of Reports ="), new XElement("Value", "0")));

                            var trackingDocument = new TrackingDocument()
                            {
                                DocumentType = documentType,
                                DocumentFormat = "XML",
                                DocumentIdentifier = header.InterchangeNo,
                                DocumentTimestamp = header.Date,
                                DestinationInterchangeNumber = header.InterchangeNo,
                                DocumentDeliveryPoint = header.MessageHeader.CustomerLocation.CustomerDeliveryPoint,
                                DestinationTradingPartnerEan = header.Receiver,
                                SourceInterchangeNumber = header.InterchangeNo,
                                SourceTradingPartnerEan = header.Sender,
                                CustomData = customData.ToString(),
                                TrackingDocumentSystem = new TrackingDocumentSystem
                                {
                                    Name = "TRANSFORMATION",
                                    GroupId = "1234567890",
                                    Timestamp = header.Date,
                                    FileInputPath = string.Empty,
                                    FileOutputPath = string.Empty,
                                    Status = "SUCCESS",
                                    StatusMessage = string.Empty
                                },
                            };


                            trackingDocument.TrackingDocumentDetails.Add(new TrackingDocumentDetail
                            {
                                DocumentType = documentType,
                                DocumentFormat = "XML",
                                DocumentTimestamp = header.Date,
                                SourceTradingPartnerEan = header.Sender,
                                DestinationTradingPartnerEan = header.Receiver,
                            });


                            db.TrackingDocuments.InsertOnSubmit(trackingDocument);
                            db.SubmitChanges();
                        }

                        ts.Complete();

                        response.ResponseResult = ResponseResult.Success;
                        response.ResponseMessage = "Delivery note saved to DB";
                        response.Trace.Add("Delivery note added");
                    }
                    catch (Exception e)
                    {
                        response.ResponseResult = ResponseResult.Exception;
                        response.ResponseMessage = "Delivery note failed saving to DB";
                        response.Trace.Add(e.Message);

                    }

                    return response;
                }
            }
        }

        private Response InsertIntoDocumentTracking(Document document, Response response)
        {
            try
            {
                using (TransactionScope ts = new TransactionScope(TransactionScopeOption.Required,
                  new TransactionOptions() { IsolationLevel = IsolationLevel.ReadUncommitted, Timeout = new TimeSpan(0, 5, 0) }))
                {

                    using (DocumentTrackingModelDataContext db = new DocumentTrackingModelDataContext())
                    {
                        try
                        {

                            var trackdoc = (from doc in db.TrackingDocuments
                                            join docsys in db.TrackingDocumentSystems on doc.System_Id equals docsys.Id
                                            where doc.DocumentType == document.Type.Trim() && doc.DocumentIdentifier == document.Number.Trim() && doc.SourceTradingPartnerEan == document.Sender && doc.DestinationTradingPartnerEan == document.Receiver && doc.DocumentDeliveryPoint == document.DeliveryPoint.Trim()
                                            select new { doc, docsys }).FirstOrDefault();
                            if (trackdoc != null)
                            {
                                TrackingDocumentSystem docsystem = (db.TrackingDocumentSystems.Where(sys => sys.Id == trackdoc.docsys.Id)).FirstOrDefault();

                                docsystem.Timestamp = document.SystemDate;

                                TrackingDocumentDetail detail = new TrackingDocumentDetail
                                {
                                    DocumentType = document.Type.Trim(),
                                    DocumentFormat = document.Format.Trim(),
                                    DocumentTimestamp = document.SystemDate,
                                    SourceTradingPartnerEan = document.Sender,
                                    DestinationTradingPartnerEan = document.Receiver,
                                    TrackingDocumentId = trackdoc.doc.Id
                                };
                                db.TrackingDocumentDetails.InsertOnSubmit(detail);
                                try
                                {
                                    db.SubmitChanges();
                                }
                                catch (Exception ex)
                                {
                                    response.ResponseResult = ResponseResult.Exception;
                                    response.ResponseMessage = "Transaction failed writing to DB";
                                    response.Trace.Add(ex.Message);
                                    return response;
                                    //foreach (ObjectChangeConflict occ in db.ChangeConflicts)
                                    //{
                                    //    //No database values are merged into current.
                                    //    occ.Resolve(RefreshMode.KeepChanges);
                                    //}
                                }
                                //response.ResponseMessage = "Transaction saved to DB";
                                //response.Trace.Add("Saving Document to DB Successful");
                                //return response;
                            }
                            else
                            {
                                try
                                {
                                    TrackingDocument TrackDocument = new TrackingDocument
                                    {

                                        DocumentType = document.Type.Trim(),
                                        DocumentFormat = document.Format.Trim(),
                                        DocumentIdentifier = document.Number.Trim(),
                                        DocumentTimestamp = document.Date,
                                        DocumentDeliveryPoint = document.DeliveryPoint.Trim(),
                                        SourceInterchangeNumber = document.InterchangeNumber,
                                        SourceTradingPartnerEan = document.Sender,
                                        DestinationInterchangeNumber = document.InterchangeNumber,
                                        DestinationTradingPartnerEan = document.Receiver,
                                        TrackingDocumentSystem = new TrackingDocumentSystem
                                        {

                                            Name = "TRANSFORMATION",
                                            GroupId = "1234567890",
                                            Timestamp = document.SystemDate,
                                            FileInputPath = "",
                                            FileOutputPath = "",
                                            Status = "SUCCESS",
                                            StatusMessage = ""

                                        },
                                        CustomData = document.CustomXML
                                    };

                                    TrackDocument.TrackingDocumentDetails.Add(new TrackingDocumentDetail()
                                    {
                                        DocumentType = document.Type.Trim(),
                                        DocumentFormat = document.Format.Trim(),
                                        DocumentTimestamp = document.SystemDate,
                                        SourceTradingPartnerEan = document.Sender,
                                        DestinationTradingPartnerEan = document.Receiver,
                                    });

                                    if (document.References != null)
                                    {

                                        foreach (var documentReference in document.References)
                                        {
                                            TrackingDocumentReference trackDocumentReference = new TrackingDocumentReference
                                            {

                                                DocumentType = documentReference.Type,
                                                DocumentIdentifier = documentReference.Number,
                                                DocumentTimestamp = documentReference.Date
                                            };
                                            TrackDocument.TrackingDocumentReferences.Add(trackDocumentReference);
                                        }
                                    }


                                    db.TrackingDocuments.InsertOnSubmit(TrackDocument);
                                    db.SubmitChanges();

                                }
                                catch (Exception e)
                                {
                                    response.ResponseResult = ResponseResult.Exception;
                                    response.ResponseMessage = "Transaction failed writing to DB";
                                    response.Trace.Add(e.Message);
                                    return response;
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            response.ResponseResult = ResponseResult.Exception;
                            response.ResponseMessage = "Transaction failed writing to DB";
                            response.Trace.Add(ex.Message);
                            return response;
                        }

                        ts.Complete();

                        response.ResponseMessage = "Transaction saved to DB";
                        response.Trace.Add("Saving Document to DB Successful");
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                response.ResponseResult = ResponseResult.Exception;
                response.ResponseMessage = "Transaction failed writing to DB";
                response.Trace.Add(ex.Message);
                return response;
            }
        }
    }
}