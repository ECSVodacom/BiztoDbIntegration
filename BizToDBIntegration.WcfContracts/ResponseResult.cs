using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BizToDBIntegration.WcfContracts
{
    [DataContract]
    public enum ResponseResult
    {
        [EnumMember]
        Success,
        [EnumMember]
        Failure,
        [EnumMember]
        Exception,
    }
}
