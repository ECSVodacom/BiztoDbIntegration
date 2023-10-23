using System;
using System.Xml;

namespace TradingGateway.Data.XsdModels
{
    internal static class Schema
    {
        public static XmlReader GetOrderSchema()
        {
            Type type = typeof(Schema);

            XmlReader reader =
                XmlReader.Create(
                    type.Assembly.GetManifestResourceStream("TradingGateway.Data.XsdModels.Order.xsd"));

            return reader;
        }
    }
}