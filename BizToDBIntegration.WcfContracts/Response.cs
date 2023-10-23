using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BizToDBIntegration.WcfContracts
{
    [DataContract]
    public sealed class Response
    {
        private List<string> trace = new List<string>();

        [DataMember]
        public ResponseResult ResponseResult;

        [DataMember]
        public string ResponseMessage { get; set; }

        [DataMember]
        public List<string> Trace
        {
            get { return trace; }
            set { trace = value; }
        }

        [DataMember]
        public string UniqueIdentifier { get; set; }

        [DataMember]
        public XmlDataType Type { get; set; }
    }
}
