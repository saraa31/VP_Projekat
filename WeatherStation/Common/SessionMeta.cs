using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [DataContract]
    public class SessionMeta
    {
        [DataMember] public string SessionId { get; set; }
        [DataMember] public DateTime StartTime { get; set; }
        [DataMember] public int ExpectedSamples { get; set; }
        [DataMember] public List<string> Headers { get; set; }
    }
}
