using System.Runtime.Serialization;

namespace BizToDBIntegration.WcfContracts
{
    [DataContract]
    public enum XmlDataType
    {
        [EnumMember]
        Order,
        [EnumMember]
        Invoice,
        [EnumMember]
        CreditNote,
        [EnumMember]
        RemittanceAdvice,
        [EnumMember]
        StoreList,
        [EnumMember]
        DocumentTracking,
        [EnumMember]
        VendorList,
        [EnumMember]
        ProductList,
        [EnumMember]
        ReconImport,
        [EnumMember]
        NRT, /* NearRealTimeTransaction */
        [EnumMember]
        DropShipOrder,
        [EnumMember]
        DropShipInvoice,
        [EnumMember]
        CreditNoteI,
        [EnumMember]
        InvoiceI,
        [EnumMember]
        Claim,
        [EnumMember]
        GRN,
        [EnumMember]
        PTHPRP,
        [EnumMember]
        DeliveryNote
    }
}
