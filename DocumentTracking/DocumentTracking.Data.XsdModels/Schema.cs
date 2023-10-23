using System;
using System.Xml;

namespace DocumentTracking.Data.XsdModels
{
    internal static class Schema
    {
        public static XmlReader GetDocTrack()
        {
            Type type = typeof(Schema);

            XmlReader reader =
                XmlReader.Create(type.Assembly.GetManifestResourceStream("DocumentTracking.Data.XsdModels.DocTrack.xsd"));

            return reader;
        }


        public static XmlReader GetPTHPRP()
        {
            Type type = typeof(Schema);

            XmlReader reader =
                XmlReader.Create(type.Assembly.GetManifestResourceStream("DocumentTracking.Data.XsdModels.PTHPRP.xsd"));

            return reader;
        }


        public static XmlReader GetDeliveryNote()
        {
            Type type = typeof(Schema);

            XmlReader reader =
                XmlReader.Create(type.Assembly.GetManifestResourceStream("DocumentTracking.Data.XsdModels.DeliveryNote.xsd"));

            return reader;
        }
    }
}