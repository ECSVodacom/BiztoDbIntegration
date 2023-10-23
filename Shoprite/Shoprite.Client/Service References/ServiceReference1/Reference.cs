﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18046
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Shoprite.Client.ServiceReference1 {
    using System.Runtime.Serialization;
    using System;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="XmlDataType", Namespace="http://schemas.datacontract.org/2004/07/BizToDBIntegration.WcfContracts")]
    public enum XmlDataType : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Order = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Invoice = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        CreditNote = 2,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        RemittanceAdvice = 3,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        StoreList = 4,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        DocumentTracking = 5,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        VendorList = 6,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        ProductList = 7,
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="Response", Namespace="http://schemas.datacontract.org/2004/07/BizToDBIntegration.WcfContracts")]
    [System.SerializableAttribute()]
    public partial class Response : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged {
        
        [System.NonSerializedAttribute()]
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string ResponseMessageField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private Shoprite.Client.ServiceReference1.ResponseResult ResponseResultField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string[] TraceField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private Shoprite.Client.ServiceReference1.XmlDataType TypeField;
        
        [System.Runtime.Serialization.OptionalFieldAttribute()]
        private string UniqueIdentifierField;
        
        [global::System.ComponentModel.BrowsableAttribute(false)]
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData {
            get {
                return this.extensionDataField;
            }
            set {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string ResponseMessage {
            get {
                return this.ResponseMessageField;
            }
            set {
                if ((object.ReferenceEquals(this.ResponseMessageField, value) != true)) {
                    this.ResponseMessageField = value;
                    this.RaisePropertyChanged("ResponseMessage");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public Shoprite.Client.ServiceReference1.ResponseResult ResponseResult {
            get {
                return this.ResponseResultField;
            }
            set {
                if ((this.ResponseResultField.Equals(value) != true)) {
                    this.ResponseResultField = value;
                    this.RaisePropertyChanged("ResponseResult");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string[] Trace {
            get {
                return this.TraceField;
            }
            set {
                if ((object.ReferenceEquals(this.TraceField, value) != true)) {
                    this.TraceField = value;
                    this.RaisePropertyChanged("Trace");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public Shoprite.Client.ServiceReference1.XmlDataType Type {
            get {
                return this.TypeField;
            }
            set {
                if ((this.TypeField.Equals(value) != true)) {
                    this.TypeField = value;
                    this.RaisePropertyChanged("Type");
                }
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string UniqueIdentifier {
            get {
                return this.UniqueIdentifierField;
            }
            set {
                if ((object.ReferenceEquals(this.UniqueIdentifierField, value) != true)) {
                    this.UniqueIdentifierField = value;
                    this.RaisePropertyChanged("UniqueIdentifier");
                }
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ResponseResult", Namespace="http://schemas.datacontract.org/2004/07/BizToDBIntegration.WcfContracts")]
    public enum ResponseResult : int {
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Success = 0,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Failure = 1,
        
        [System.Runtime.Serialization.EnumMemberAttribute()]
        Exception = 2,
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="ServiceReference1.IImportData")]
    public interface IImportData {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IImportData/ImportXml", ReplyAction="http://tempuri.org/IImportData/ImportXmlResponse")]
        Shoprite.Client.ServiceReference1.Response ImportXml(Shoprite.Client.ServiceReference1.XmlDataType type, string uniqueIdentifier, System.Xml.Linq.XElement data);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IImportData/ImportXml", ReplyAction="http://tempuri.org/IImportData/ImportXmlResponse")]
        System.Threading.Tasks.Task<Shoprite.Client.ServiceReference1.Response> ImportXmlAsync(Shoprite.Client.ServiceReference1.XmlDataType type, string uniqueIdentifier, System.Xml.Linq.XElement data);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IImportDataChannel : Shoprite.Client.ServiceReference1.IImportData, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class ImportDataClient : System.ServiceModel.ClientBase<Shoprite.Client.ServiceReference1.IImportData>, Shoprite.Client.ServiceReference1.IImportData {
        
        public ImportDataClient() {
        }
        
        public ImportDataClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public ImportDataClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ImportDataClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public ImportDataClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public Shoprite.Client.ServiceReference1.Response ImportXml(Shoprite.Client.ServiceReference1.XmlDataType type, string uniqueIdentifier, System.Xml.Linq.XElement data) {
            return base.Channel.ImportXml(type, uniqueIdentifier, data);
        }
        
        public System.Threading.Tasks.Task<Shoprite.Client.ServiceReference1.Response> ImportXmlAsync(Shoprite.Client.ServiceReference1.XmlDataType type, string uniqueIdentifier, System.Xml.Linq.XElement data) {
            return base.Channel.ImportXmlAsync(type, uniqueIdentifier, data);
        }
    }
}
