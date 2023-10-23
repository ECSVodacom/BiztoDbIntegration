using System;
using System.Xml;

namespace Shoprite.Data.XsdModels
{
    internal static class Schema
    {
        public static XmlReader GetInvoiceSchema()
        {
            Type type = typeof (Schema);

            XmlReader reader =
                XmlReader.Create(type.Assembly.GetManifestResourceStream("Shoprite.Data.XsdModels.EnterpriseDocument.xsd"));

            return reader;
        }
    }
}