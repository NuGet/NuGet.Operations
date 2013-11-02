using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Storage
{
    public abstract class StorageContainer
    {
        public abstract Task<StorageFileReference> GetFile(string name);
    }
}
