using System.ServiceModel;
using Common.Faults;

namespace Common
{
    [ServiceContract]
    public interface IWeatherService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string StartSession(SessionMeta meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string PushSample(WeatherSample sample);

        [OperationContract]
        string EndSession();
    }
}
