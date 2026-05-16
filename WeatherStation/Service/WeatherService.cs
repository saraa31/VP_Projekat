using System;
using System.Globalization;
using System.ServiceModel;
using System.Configuration;
using Common;
using Common.Faults;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class WeatherService : IWeatherService
    {
        private SessionWriter _measurementsWriter;
        private SessionWriter _rejectsWriter;
        private string _sessionId;
        private bool _sessionActive = false;

        private WeatherEventPublisher _publisher = new WeatherEventPublisher();
        private WeatherEventObserver _observer;

        private float _prevT;
        private float _sumT = 0;
        private int _countS = 0;
        private float _tThreshold;

        private float _prevRh;
        private float _prevDew;
        private float _rhThreshold;
        private float _dewThreshold;
        private float _outOfBandFactor;

        public WeatherService()
        {
            _observer = new WeatherEventObserver(_publisher);

            var culture = CultureInfo.InvariantCulture;
            _tThreshold = float.Parse(ConfigurationManager.AppSettings["T_threshold"], culture);
            _rhThreshold = float.Parse(ConfigurationManager.AppSettings["RH_threshold"], culture);
            _dewThreshold = float.Parse(ConfigurationManager.AppSettings["DEW_threshold"], culture);
            float outOfBandPercent = float.Parse(ConfigurationManager.AppSettings["OutOfBand_percent"], culture);
            _outOfBandFactor = outOfBandPercent / 100f;
        }
        public string StartSession(SessionMeta meta)
        {
            if(meta == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault 
                { 
                    Message = "Session metadata cannot be null." 
                });

            string[] requiredHeaders = { "Date", "T", "Pressure", "Tpot", "Tdew", "Rh", "Sh" };

            if (meta.Headers == null || meta.Headers.Count == 0)
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Message = "Session headers cannot be empty."
                });

            foreach (string required in requiredHeaders)
            {
                if (!meta.Headers.Contains(required))
                    throw new FaultException<DataFormatFault>(new DataFormatFault
                    {
                        Message = $"Missing required header: {required}"
                    });
            }

            _sessionId = meta.SessionId;

            string measurementsFilePath = $"measurements_{_sessionId}.csv";
            string rejectsFilePath = $"rejects_{_sessionId}.csv";

            _measurementsWriter = new SessionWriter(measurementsFilePath);
            _rejectsWriter = new SessionWriter(rejectsFilePath);

            _measurementsWriter.WriteSessionData("Date,T,Pressure,Tpot,Tdew,Rh,Sh");
            _rejectsWriter.WriteSessionData("Date,T,Pressure,Tpot,Tdew,Rh,Sh,Reason");

            _sessionActive = true;
            Console.WriteLine($"Session started with ID: {_sessionId}, Start Time: {meta.StartTime}, Expected Samples: {meta.ExpectedSamples}");

            _publisher.RaiseTransferStarted(_sessionId);

            return "ACK";
        }
        public string PushSample(WeatherSample sample)
        {
            if (!_sessionActive)
                return "NACK|Session is not active";
           
            if (sample == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Message = "Sample cannot be null."
                });

            if (string.IsNullOrEmpty(sample.Date))
                throw new FaultException<DataFormatFault>(new DataFormatFault
                {
                    Message = "Date is required."
                });

            if (sample.T < -90 || sample.T > 60)
            {
                _rejectsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},Temperature out of range");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Temperature is out of range: {sample.T}"
                });
            }

            if (sample.Rh <= 0 || sample.Rh > 100)
            {
                _rejectsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},Rh out of range");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Relative humidity is out of range: {sample.Rh}"
                });
            }

            if (sample.Pressure <= 0 || sample.Pressure > 1200)
            {
                _rejectsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},Pressure out of range");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Pressure is out of range: {sample.Pressure}"
                });
            }

            if (sample.Tdew < -80 || sample.Tdew > 35)
            {
                _rejectsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},Tdew out of range");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Dew point is out of range: {sample.Tdew}"
                });
            }

            if (sample.Tdew > sample.T)
            {
                _rejectsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},Tdew cannot exceed T");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Dew point ({sample.Tdew}) cannot be higher than temperature ({sample.T})"
                });
            }

            if (sample.Tpot < 200 || sample.Tpot > 350)
            {
                _rejectsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},Tpot out of range");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Potential temperature is out of range: {sample.Tpot}"
                });
            }

            if (sample.Sh < 0 || sample.Sh > 30)
            {
                _rejectsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},Sh out of range");
                throw new FaultException<ValidationFault>(new ValidationFault
                {
                    Message = $"Specific humidity is out of range: {sample.Sh}"
                });
            }

            _measurementsWriter.WriteSessionData($"{sample.Date},{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh}");

            Console.WriteLine($"Transfer in progress... | Sample received: Date={sample.Date}, T={sample.T}");

            _publisher.RaiseSampleReceived(sample);
            CheckTemperature(sample);
            CheckHumidityAndDew(sample);

            return "ACK|IN_PROGRESS";
        }
        public string EndSession()
        {
            if (!_sessionActive)
                return "NACK|No active session";
            _sessionActive = false;

            _measurementsWriter?.Dispose();
            _rejectsWriter?.Dispose();

            Console.WriteLine("Transfer completed.");

            _publisher.RaiseTransferCompleted(_sessionId);

            return "ACK|COMPLETED";
        }       
        private void CheckTemperature(WeatherSample sample)
        {
            float temp = sample.T;

            _sumT += temp;
            _countS++;
            float meanT = _sumT / _countS;

            if(_countS > 1)
            {
                float deltaT = temp - _prevT;
                if(Math.Abs(deltaT) > _tThreshold)
                {
                    string direction;
                    if (deltaT > 0)
                        direction = "above expected";
                    else
                        direction = "below expected";

                    _publisher.RaiseWarning("TemperatureSpike", direction, $"DeltaT={deltaT:F2}°C (threshold={_tThreshold}), T={temp}°C");
                }
                if(temp < (1 - _outOfBandFactor) * meanT)
                    _publisher.RaiseWarning("OutOfBandWarning", "below expected", $"T={temp}°C < 75% of mean ({meanT:F2}°C)");
                else if(temp > (1 + _outOfBandFactor) * meanT)
                    _publisher.RaiseWarning("OutOfBandWarning", "above expected", $"T={temp}°C > 125% of mean ({meanT:F2}°C)");
            }

            _prevT = temp;
        }
        private void CheckHumidityAndDew(WeatherSample sample)
        {
            if(_countS > 1)
            {
                float deltaRh = sample.Rh - _prevRh;
                if(Math.Abs(deltaRh) > _rhThreshold)
                {
                    string direction;
                    if (deltaRh > 0)
                        direction = "above expected";
                    else
                        direction = "below expected";
                    _publisher.RaiseWarning("RHSpike", direction, $"DeltaRH={deltaRh:F2}% (threshold={_rhThreshold}), Rh={sample.Rh}%");
                }
                float deltaDew = sample.Tdew - _prevDew;
                if(Math.Abs(deltaDew) > _dewThreshold)
                {
                    string direction;
                    if (deltaDew > 0)
                        direction = "above expected";
                    else
                        direction = "below expected";
                    _publisher.RaiseWarning("DEWSpike", direction, $"DeltaTdew={deltaDew:F2}°C (threshold={_dewThreshold}), Tdew={sample.Tdew}°C");
                }
            }

            _prevRh = sample.Rh;
            _prevDew = sample.Tdew;
        }
    }
}
