using Common;
using Common.Faults;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(baseDir, "..", "..", "Data", "cleaned_weather.csv");
            csvPath = Path.GetFullPath(csvPath);
            string logPath = "invalid_samples.log";

            List<WeatherSample> samples = CSVParser.LoadSamples(csvPath, logPath);
            Console.WriteLine($"Loaded {samples.Count} valid rows.");

            ChannelFactory<IWeatherService> factory = new ChannelFactory<IWeatherService>("WeatherService");
            IWeatherService proxy = factory.CreateChannel();

            bool aborted = false;
            try
            {
                SessionMeta meta = new SessionMeta
                {
                    SessionId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.Now,
                    ExpectedSamples = samples.Count,
                    Headers = new List<string> { "Date", "T", "Pressure", "Tpot", "Tdew", "Rh", "Sh" }
                };

                string ack = proxy.StartSession(meta);
                Console.WriteLine($"StartSession: {ack}");

                Console.WriteLine("Transfer in progress...");
                for (int i = 0; i < samples.Count; i++)
                {
                    /*
                    if(i == samples.Count / 2)
                    {
                        Console.WriteLine("[SIMULATION] Connection lost mid-transfer. Closing resources...");
                        ((IClientChannel)proxy).Abort();
                        aborted = true;
                        break;
                    }*/

                    try
                    {
                        string result = proxy.PushSample(samples[i]);
                        Console.WriteLine($"[{i + 1}/{samples.Count}] PushSample: {result}");
                    }
                    catch (FaultException<ValidationFault> ex)
                    {
                        Console.WriteLine($"[{i + 1}/{samples.Count}] NACK | ValidationFault: {ex.Detail.Message}");
                    }
                    catch (FaultException<DataFormatFault> ex)
                    {
                        Console.WriteLine($"[{i + 1}/{samples.Count}] NACK | DataFormatFault: {ex.Detail.Message}");
                    }
                    System.Threading.Thread.Sleep(50);
                }
                if (!aborted)
                {
                    Console.WriteLine("Transfer completed.");
                    string done = proxy.EndSession();
                    Console.WriteLine($"EndSession: {done}");
                }
                else
                {
                    Console.WriteLine("EndSession: Session interrupted due to connection failure.");
                }
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"DataFormatFault: {ex.Detail.Message}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"ValidationFault: {ex.Detail.Message}");
            }
            finally
            {
                try
                {
                    ((IClientChannel)proxy).Close();
                    factory.Close();
                }
                catch
                {
                    ((IClientChannel)proxy).Abort();
                }

                Console.WriteLine("[DISPOSE] Channel and factory closed - all resources disposed.");
                Console.ReadKey();
            }
        }
    }
}
