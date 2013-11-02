using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Services.Storage
{
    public abstract class StorageAccount
    {
        public abstract Task<StorageContainer> GetContainerReference(string name);
    }
}
