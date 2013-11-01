using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Communications
{
    public struct FailureRateThreshold
    {
        public TimeSpan Period { get; private set; }
        public double Rate { get; private set; }

        public FailureRateThreshold(double rate, TimeSpan period) : this()
        {
            Rate = rate;
            Period = period;
        }
    }
}
