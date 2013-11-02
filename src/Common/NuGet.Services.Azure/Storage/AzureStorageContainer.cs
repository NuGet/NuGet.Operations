using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.Services.Storage
{
    public class AzureStorageContainer : StorageContainer
    {
        public CloudBlobContainer Container { get; private set; }

        public AzureStorageContainer(CloudBlobContainer container)
        {
            Container = container;
        }

        public override Task<StorageFileReference> GetFile(string name)
        {
            return Task.FromResult<StorageFileReference>(new AzureStorageFileReference(Container.GetBlockBlobReference(name)));
        }
    }
}
