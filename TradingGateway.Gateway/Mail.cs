using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace TradingGateway.Gateway
{
    public class Mail
    {
        private StringBuilder Body { get; set; }
        private string Subject { get; set; }

        public Mail()
        {
        }

        public string Send(List<Order> orderRecords)
        {
            using (TradingGatewayOrderDataContext data = new TradingGatewayOrderDataContext())
            {
                try
                {
                    var orderRecord = (from o in orderRecords
                                       select o).FirstOrDefault();

                    var details =
                        data.GetEmailDetails(orderRecord.SupplierEAN,
                            orderRecord.DestinationRetailerEAN,
                            orderRecord.OrderingRetailerEAN).FirstOrDefault();

                    if (!(bool)details.IsEmailNotificationEnabled && details.EmailToAddress == String.Empty)
                        return "Email notifications is disabled or no email is address supplied";

                    var body = new StringBuilder(String.Format("Dear {0} User", details.SupplierName));
                    body.AppendLine(Environment.NewLine);
                    string orderNumbers = string.Empty;

                    if (orderRecords.Count() > 1)
                    {
                        body.AppendLine(
                           String.Format("New orders arrived from {0}.",
                                          details.OrderingRetailer));
                        body.AppendLine(Environment.NewLine);
                        body.AppendLine(Environment.NewLine);
                        body.AppendLine(String.Format("Click here <{0}/Dashboard/Index>  to view this order:", details.UriRoot));
                        Subject = String.Format("Trading Gateway New Orders ({0})",
                                                DateTime.Now.ToString("MMM dd yyyy HH:mm"));
                    }
                    else
                    {
                        body.AppendLine(
                            String.Format("A new order arrived from {0}. Please find below some order details as a reference",
                                           details.OrderingRetailer));
                        body.AppendLine(Environment.NewLine);
                        body.AppendLine(String.Format("*\t Order Number: {0}", orderRecord.RetailerOrderNumber));
                        body.AppendLine(String.Format("*\t Store: {0}", details.DestinationRetailer));
                        body.AppendLine(String.Format("*\t Date of order: {0}", orderRecord.OrderDate.ToString()));
                        body.AppendLine(String.Format("*\t Interchange No: {0}", orderRecord.InterchangeNo));
                        body.AppendLine(String.Format("*\t Earliest delivery date: {0}", orderRecord.EarliestDeliveryDate.ToString()));
                        body.AppendLine(String.Format("*\t Latest delivery date: {0}", orderRecord.LatestDeliveryDate.ToString()));
                        body.AppendLine(String.Format("*\t Narrative: {0}", orderRecord.Narrative));
                        body.AppendLine(Environment.NewLine);
                        body.AppendLine(String.Format("Click here <{0}/Order/Details?orderId={1}>  to view this order:", details.UriRoot, orderRecord.OrderId));
                        Subject = String.Format("Trading Gateway New Order ({0})",
                                                DateTime.Now.ToString("MMM dd yyyy HH:mm"));
                    }

                    Body = body;

                    var mailMessage =
                        new MailMessage
                        {
                            From = new MailAddress(details.EmailAddressFrom,
                                    String.Format("A new order arrived from {0}", details.OrderingRetailer)),
                            Subject = Subject,
                            Body = Body.ToString()
                        };

                    string[] to = details.EmailToAddress.Split(';');
                    foreach (string t in to.Where(t => t != String.Empty))
                    {
                        mailMessage.To.Add(t);
                    }

                    var smtpClient = new SmtpClient
                    {
                        Host = details.SmtpHost,
                        Port = 25
                    };

                    smtpClient.Send(mailMessage);
                }
                catch (Exception exception)
                {

                    return String.Format("Unable to send email. {0}", exception.Message);
                }

                return "Email sent";
            }
        }


    }
}
