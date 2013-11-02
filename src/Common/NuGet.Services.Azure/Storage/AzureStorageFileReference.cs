using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NuGet.Services.Storage
{
    public class AzureStorageFileReference : StorageFileReference
    {
        public CloudBlockBlob Blob { get; private set; }

        public override string Name { get { return Blob.Name; } }
        public override string Etag { get { return Blob.Properties.ETag; } }
        public override DateTimeOffset? LastModified { get { return Blob.Properties.LastModified; } }

        public AzureStorageFileReference(CloudBlockBlob blob)
        {
            Blob = blob;
        }

        public override Task<bool> Exists()
        {
            // Do this, but wrapped in the fail fast manager/request pipeline
            //return Task.Factory.FromAsync(
            //    (cb, state) => Blob.BeginExists(cb, state),
            //    ar => Blob.EndExists(ar),
            //    state: null);
            throw new NotImplementedException();
        }
    }
}
