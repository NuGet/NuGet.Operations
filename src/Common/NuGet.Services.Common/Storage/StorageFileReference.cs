using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Storage
{
    public abstract class StorageFileReference
    {
        public abstract string Name { get; }
        public abstract string Etag { get; }
        public abstract DateTimeOffset? LastModified { get; }

        public abstract Task<bool> Exists();
    }
}
