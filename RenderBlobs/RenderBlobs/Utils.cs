using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace RenderBlobs
{
    class Utils
    {
        public static async Task CreateBlob(StorageAddress storage, string name, string contentType, string content)
        {
            name = name.ToLowerInvariant();

            UTF8Encoding encoding = new UTF8Encoding();
            MemoryStream stream = new MemoryStream(encoding.GetBytes(content));

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storage.ConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(storage.Container);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(name);
            blockBlob.Properties.ContentType = contentType;
            blockBlob.Properties.CacheControl = "no-cache, no-store, must-revalidate";

            await Task.Factory.FromAsync(blockBlob.BeginUploadFromStream(stream, null, null), blockBlob.EndUploadFromStream);

            //Console.WriteLine("{0}", name);
        }
    }
}
