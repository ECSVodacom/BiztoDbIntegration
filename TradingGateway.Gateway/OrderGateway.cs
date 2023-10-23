using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Transactions;
using BizToDBIntegration.WcfContracts;

namespace TradingGateway.Gateway
{
    internal sealed class OrderGateway
    {
        public Response Save(string xml, Response returnResponse)
        {
            using (var ts = new TransactionScope())
            {
                try
                {
                    Order orderRecord = null;
                    List<Order> orderRecords = new List<Order>();

                    using (var db = new TradingGatewayOrderDataContext())
                    {


                        var orderLinqXml = OrderInsertReq.Parse(xml);

                        foreach (var orders in orderLinqXml.OrderInsert)
                        {

                            //var orders = (from ord in orderLinqXml.OrderInsert
                            //              select ord).SingleOrDefault();

                            var duplicateOrder = (from order in db.Orders
                                                  where order.RetailerOrderNumber == orders.OrderReference_CustomerOrderNo.Trim()
                                                    && order.DestinationRetailerEAN == orders.CustomerDeliveryPoint.Trim()
                                                  select order
                                                  ).FirstOrDefault();

                            if (duplicateOrder != null)
                            {
                                returnResponse.ResponseResult = ResponseResult.Success;
                                returnResponse.ResponseMessage = "Order failed to save";
                                returnResponse.Trace.Add(string.Format("This would result in a duplicate order {0} for destination {1}",
                                    orders.OrderReference_CustomerOrderNo.Trim(),
                                    orders.CustomerDeliveryPoint.Trim()));

                                continue;
                                //return returnResponse;
                            }

                            orderRecord = new Order
                            {
                                InterchangeNo = orders.InterchangeNo.Trim(),
                                SupplierEAN = orders.SupplierOrderPoint.Trim(),
                                OrderingRetailerEAN = orders.CustomerOrderpoint.Trim(),
                                DestinationRetailerEAN = orders.CustomerDeliveryPoint.Trim(),
                                RetailerOrderNumber = orders.OrderReference_CustomerOrderNo.Trim(),
                                OrderDate = Convert.ToDateTime(orders.OrderReference_OrderDate.Trim()),
                                TransactionCode = Convert.ToChar(orders.OrderReference_TransactionCode.Trim()),
                                OrderStatusId = 1,
                                OrderStatusChangeDate = DateTime.Now,
                                Narrative = orders.Narrative.Trim(),
                                AlternativeOrderPoint = orders.AlternativeOrderPoint
                            };

                            if (!String.IsNullOrEmpty(orders.DeliveryDate_Earliest))
                                orderRecord.EarliestDeliveryDate = Convert.ToDateTime(orders.DeliveryDate_Earliest.Trim());
                            if (!String.IsNullOrEmpty(orders.DeliveryDate_Latest))
                                orderRecord.LatestDeliveryDate = Convert.ToDateTime(orders.DeliveryDate_Latest.Trim());

                            //var lineItems =
                            //    (from ord in orderLinqXml.OrderInsert
                            //     from lineItem in orders.Order_LineItemInsert
                            //     select lineItem);

                            decimal? totalOrderValue = 0;
                            foreach (var orderLine in orders.Order_LineItemInsert.Select(lineItem => new OrderLine
                            {
                                SequenceNumber = Convert.ToInt32(lineItem.LineSequenceNo),
                                PackBarcode = lineItem.ConsumerBarcode,
                                UnitBarcode = lineItem.OrderUnitBarcode,
                                SupplierProductCode = lineItem.ProductCode,
                                ProductDescription = lineItem.ProductDescription,
                                Quantity = String.IsNullOrEmpty(lineItem.OrderQuantity) ? 0 : Convert.ToDecimal(lineItem.OrderQuantity, CultureInfo.InvariantCulture),
                                CostPrice = String.IsNullOrEmpty(lineItem.OrderCostPrice) ? 0 : Convert.ToDecimal(lineItem.OrderCostPrice, CultureInfo.InvariantCulture),
                                VatRate = String.IsNullOrEmpty(lineItem.VatRatePercentage) ? 0 : Convert.ToDecimal(lineItem.VatRatePercentage, CultureInfo.InvariantCulture),
                                LineCost = String.IsNullOrEmpty(lineItem.OrderLineCost) ? 0 : Convert.ToDecimal(lineItem.OrderLineCost, CultureInfo.InvariantCulture),
                                PackSize = String.IsNullOrEmpty(lineItem.PackSize) ? 1 : Convert.ToDecimal(lineItem.PackSize, CultureInfo.InvariantCulture),
                                Narrative = String.IsNullOrEmpty(lineItem.LineNarrative) ? String.Empty : lineItem.LineNarrative

                            }))
                            {
                                totalOrderValue += orderLine.LineCost;

                                orderRecord.OrderLines.Add(orderLine);
                            }

                            orderRecord.TotalOrderValue = totalOrderValue;


                            db.Orders.InsertOnSubmit(orderRecord);
                            db.SubmitChanges();

                            orderRecords.Add(orderRecord);

                            returnResponse.ResponseResult = ResponseResult.Success;
                            returnResponse.Trace.Add(string.Format("Order {0} for destination {1} saved",
                                orders.OrderReference_CustomerOrderNo.Trim(),
                                orders.CustomerDeliveryPoint.Trim()));


                        }

                        returnResponse.ResponseResult = ResponseResult.Success;
                        returnResponse.ResponseMessage = "Transaction saved to DB";
                        returnResponse.Trace.Add("Saving order(s) to DB Successful");
                    }


                    try
                    {
                        Mail mail = new Mail();


                        mail.Send(orderRecords);

                    }
                    catch (Exception ex)
                    {
                        returnResponse.Trace.Add("Mail could not be sent");
                    }

                    return returnResponse;
                }
                catch (Exception ex)
                {
                    returnResponse.ResponseResult = ResponseResult.Exception;
                    returnResponse.ResponseMessage = "Transaction failed writing to DB";
                    returnResponse.Trace.Add(ex.Message);

                    return returnResponse;
                }
                finally
                {
                    ts.Complete();
                }
            }
        }
    }
}