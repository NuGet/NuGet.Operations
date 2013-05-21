using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderBlobs
{
    class PackageExplorer
    {
        public static void RenderSinglePackageContent(Stream stream, TextWriter writer)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Contents contents = new Contents(archive.Entries);
                JToken json = contents.ToJson();
                writer.Write(json);
            }
        }

        public static async Task RenderPackageContent(string packageConnectionString, string storageConnectionString)
        {
            CloudStorageAccount packageAccount = CloudStorageAccount.Parse(packageConnectionString);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            CreateContainerIfNotExists(storageAccount);

            CloudBlobClient blobClient = packageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("packages");
            int count = 0;
            foreach (CloudBlockBlob item in blobContainer.ListBlobs(useFlatBlobListing: true))
            {
                count++;

                if (count % 1000 == 0)
                {
                    Console.WriteLine("{0}", count);
                }

                await PackageContentBlob(storageAccount, item);
            }
        }

        private static async Task PackageContentBlob(CloudStorageAccount storageAccount, CloudBlockBlob blockBlob)
        {
            MemoryStream stream = new MemoryStream();

            await Task.Factory.FromAsync(blockBlob.BeginDownloadToStream(stream, null, null), blockBlob.EndDownloadToStream);

            stream.Seek(0, SeekOrigin.Begin);

            Console.WriteLine(blockBlob.Name);

            try
            {
                JToken json;
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    Contents contents = new Contents(archive.Entries);
                    json = contents.ToJson();
                }

                await CreateBlob(storageAccount, "packagecontent", blockBlob.Name + ".json", "application/json", json.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception: {1}", blockBlob.Name, e.Message);
            }
        }

        private static async Task CreateBlob(CloudStorageAccount storageAccount, string container, string name, string contentType, string content)
        {
            name = name.ToLowerInvariant();

            UTF8Encoding encoding = new UTF8Encoding();
            MemoryStream stream = new MemoryStream(encoding.GetBytes(content));

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(container);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(name);
            blockBlob.Properties.ContentType = contentType;
            blockBlob.Properties.CacheControl = "no-cache, no-store, must-revalidate";

            await Task.Factory.FromAsync(blockBlob.BeginUploadFromStream(stream, null, null), blockBlob.EndUploadFromStream);

            //Console.WriteLine("{0}", name);
        }

        private static void CreateContainerIfNotExists(CloudStorageAccount storageAccount)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("packagecontent");

            container.CreateIfNotExists();  // this can throw if the container was just deleted a few seconds ago

            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
        }
    }
}

