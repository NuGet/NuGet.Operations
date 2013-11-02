using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services
{
    internal class TestClock : Clock
    {
        private DateTimeOffset _utcNow;

        public override DateTimeOffset UtcNow { get { return _utcNow; } }

        public TestClock() { _utcNow = DateTimeOffset.UtcNow; }

        public virtual TestClock Advance(TimeSpan time)
        {
            _utcNow += time;
            return this;
        }

        public virtual TestClock Advance(int ms)
        {
            return Advance(TimeSpan.FromMilliseconds(ms));
        }
    }
}
