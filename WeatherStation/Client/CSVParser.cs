using Common;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Client
{
    public class CSVParser
    {
        private const int MAX_ROWS = 100;

        public static List<WeatherSample> LoadSamples(string filePath, string logPath)
        {
            List<WeatherSample> samples = new List<WeatherSample>();
            List<string> invalidSamples = new List<string>();

            using (CSVReader reader = new CSVReader(filePath))
            {
                string header = reader.ReadLine();

                int rowNumber = 0;
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    rowNumber++;

                    if (rowNumber > MAX_ROWS)
                    {
                        invalidSamples.Add($"Row {rowNumber}: EXCESS ROW (limit is {MAX_ROWS}) | {line}");
                        continue;
                    }

                    string error;
                    WeatherSample sample = TryParse(line, rowNumber, out error);
                    if (sample == null)
                        invalidSamples.Add($"Row {rowNumber}: {line} | Reason: {error}");
                    else
                        samples.Add(sample);
                }
            }

            if (invalidSamples.Count > 0)
                File.WriteAllLines(logPath, invalidSamples);
            else
                File.WriteAllText(logPath, "No invalid or excess rows found.");

            return samples;
        }

        private static WeatherSample TryParse(string line, int rowNumber, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(line))
            {
                error = "Empty or whitespace line";
                return null;
            }

            string[] lineParts = line.Split(',');

            if (lineParts.Length < 10)
            {
                error = $"Too few columns ({lineParts.Length}). Expected at least 10.";
                return null;
            }

            var culture = CultureInfo.InvariantCulture;

            float pressure, t, tpot, tdew, rh, sh;

            if (!float.TryParse(lineParts[1].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, culture, out pressure))
            {
                error = $"Pressure parse failed: '{lineParts[1].Trim()}'";
                return null;
            }
            if (!float.TryParse(lineParts[2].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, culture, out t))
            {
                error = $"T parse failed: '{lineParts[2].Trim()}'";
                return null;
            }
            if (!float.TryParse(lineParts[3].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, culture, out tpot))
            {
                error = $"Tpot parse failed: '{lineParts[3].Trim()}'";
                return null;
            }
            if (!float.TryParse(lineParts[4].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, culture, out tdew))
            {
                error = $"Tdew parse failed: '{lineParts[4].Trim()}'";
                return null;
            }
            if (!float.TryParse(lineParts[5].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, culture, out rh))
            {
                error = $"Rh parse failed: '{lineParts[5].Trim()}'";
                return null;
            }
            if (!float.TryParse(lineParts[9].Trim(), NumberStyles.Float | NumberStyles.AllowThousands, culture, out sh))
            {
                error = $"Sh parse failed: '{lineParts[9].Trim()}'";
                return null;
            }

            string date = lineParts[0].Trim();
            if (string.IsNullOrEmpty(date))
            {
                error = "Date is empty";
                return null;
            }

            return new WeatherSample
            {
                Date = date,
                Pressure = pressure,
                T = t,
                Tpot = tpot,
                Tdew = tdew,
                Rh = rh,
                Sh = sh
            };
        }
    }
}
