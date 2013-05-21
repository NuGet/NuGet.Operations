using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ApiProto
{
    public class AzureBlobStorage : IStorage
    {
        public AzureBlobStorage(string connectionString, string container)
        {
            ConnectionString = connectionString;
            Container = container;
        }

        public string ConnectionString { get; private set; }

        public string Container { get; private set; }

        public async Task<string> Load(string name)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(Container);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(name);

            MemoryStream stream = new MemoryStream();

            await Task.Factory.FromAsync(blockBlob.BeginDownloadToStream(stream, null, null), blockBlob.EndDownloadToStream);

            stream.Seek(0, SeekOrigin.Begin);

            string content;
            using (TextReader reader = new StreamReader(stream))
            {
                content = reader.ReadToEnd();
            }

            return content;
        }
    }
}