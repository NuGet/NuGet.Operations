using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Work.Jobs.Models
{
    public class PackageRef
    {
        public PackageRef(string id, string version, string hash, DateTime lastEdited)
        {
            Id = id;
            Version = version;
            Hash = hash;
            LastEdited = lastEdited;
        }
        public string Id { get; set; }
        public string Version { get; set; }
        public string Hash { get; set; }
        public DateTime? LastEdited { get; set; }
    }
}
