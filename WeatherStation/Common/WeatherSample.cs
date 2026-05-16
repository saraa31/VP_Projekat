using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class WeatherSample
    {
        [DataMember] public float T { get; set; }
        [DataMember] public float Pressure { get; set; }
        [DataMember] public float Tpot { get; set; }
        [DataMember] public float Tdew { get; set; }
        [DataMember] public float Rh { get; set; }
        [DataMember] public float Sh { get; set; }
        [DataMember] public string Date { get; set; }
    }
}
