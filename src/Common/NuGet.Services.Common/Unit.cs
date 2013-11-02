using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public class Unit
    {
        public static readonly Unit Instance = new Unit();

        private Unit() { }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(obj, Instance);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "<unit>";
        }
    }
}
