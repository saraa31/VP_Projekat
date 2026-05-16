using System;
using Common;

namespace Service
{
    public class SampleEventArgs : EventArgs
    {
        public WeatherSample Sample { get; }
        public SampleEventArgs(WeatherSample sample)
        {
            Sample = sample;
        }
    }
}
