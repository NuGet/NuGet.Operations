using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Work.Jobs.Models
{
    public class PackageRef
    {
        public PackageRef(string id, string version, string hash)
        {
            Id = id;
            Version = version;
            Hash = hash;
        }
        public string Id { get; set; }
        public string Version { get; set; }
        public string Hash { get; set; }
    }

    public class PackageRefWithLastEdited : PackageRef
    {
        public PackageRefWithLastEdited(string id, string version, string hash, DateTime lastEdited)
            : base(id, version, hash)
        {
            LastEdited = lastEdited;
        }

        public DateTime LastEdited { get; set; }
    }
}
