using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services
{
    internal class Clock
    {
        public static readonly Clock Instance = new Clock();

        public virtual DateTimeOffset UtcNow { get { return DateTimeOffset.UtcNow; } }

        protected Clock() { }
    }
}
