using System;
using System.Xml;

namespace Spar.Data.XsdModels
{
    internal static class Schema
    {
        public static XmlReader GetRemittanceAdviceSchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.RemittanceAdvice.RemittanceAdvice.xsd"));

            return reader;
        }

        public static XmlReader GetRemittanceAdviceSchemaOldDC()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.RemittanceAdvice.RemittanceAdviceOldDC.xsd"));

            return reader;
        }

        public static XmlReader GetInvoiceISchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.Invoices.InvoiceI.xsd"));

            return reader;
        }

        public static XmlReader GetClaimSchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.Claims.Claim.xsd"));

            return reader;
        }

        public static XmlReader GetCreditNoteISchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.CreditNotes.CreditNoteI.xsd"));

            return reader;
        }

        public static XmlReader GetRemittanceAdviceSchemaOldDCCommission()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels..xsd"));

            return reader;
        }

        public static XmlReader GetRemittanceAdviceSchemaOldDS()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.RemittanceAdvice.RemittanceAdviceOld.xsd"));

            return reader;
        }

        public static XmlReader GetStoreListSchema()
        {
            var type = typeof(Schema);


            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.StoreList.Stores.xsd"));

            return reader;
        }

        internal static XmlReader GetVendorListSchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.VendorList.SPARVendors.xsd"));

            return reader;
        }

        internal static XmlReader GetReconImportSchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.ReconReport.ReconWeeklyImport.xsd"));

            return reader;
        }

        internal static XmlReader GetOrderDSHSchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.Orders.SparDSHOrder.xsd"));

            return reader;
        }

        internal static XmlReader GetInvoiceDSHSchema()
        {
            var type = typeof(Schema);

            var reader = XmlReader.Create(type.Assembly.GetManifestResourceStream("Spar.Data.XsdModels.Invoices.SparDSHInvoice.xsd"));

            return reader;
        }
    }
}
