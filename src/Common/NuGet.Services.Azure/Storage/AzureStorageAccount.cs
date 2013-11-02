using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.Services.Storage
{
    public class AzureStorageAccount : StorageAccount
    {
        private readonly CloudBlobClient _blobs;

        public CloudStorageAccount Account { get; private set; }

        public AzureStorageAccount(CloudStorageAccount account)
        {
            Account = account;
            _blobs = account.CreateCloudBlobClient();
        }

        public override Task<StorageContainer> GetContainerReference(string name)
        {
            return Task.FromResult<StorageContainer>(new AzureStorageContainer(_blobs.GetContainerReference(name)));
        }
    }
}
