using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace RenderBlobs
{
    static class StorageGallery
    {
        public static async Task Save(string connectionString, Gallery gallery, bool isDryRun = false)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            await Task.WhenAll(
                SavePackageRegistrations(storageAccount, gallery, isDryRun),
                SaveOwners(storageAccount, gallery, isDryRun),
                SavePackages(storageAccount, gallery, isDryRun));

            //await SaveOwners(storage, gallery, isDryRun);
        }

        private static async Task SavePackageRegistrations(CloudStorageAccount storageAccount, Gallery gallery, bool isDryRun)
        {
            foreach (Gallery.PackageRegistration packageRegistration in gallery.PackageRegistrations.Values)
            {
                await SavePackageRegistration(storageAccount, packageRegistration, isDryRun);
            }
        }

        private static async Task SavePackageRegistration(CloudStorageAccount storageAccount, Gallery.PackageRegistration packageRegistration, bool isDryRun)
        {
            try
            {
                JToken root = packageRegistration.CreateDocument();

                // this should never be null but we can protect ourselves from problems in the data or bugs in our SQL
                if (root != null)
                {
                    if (!isDryRun)
                    {
                        await CreateBlob(storageAccount, "apiv3", packageRegistration.Name, "application/json", root.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(packageRegistration.Id, e);
            }
        }

        private static async Task SaveOwners(CloudStorageAccount storageAccount, Gallery gallery, bool isDryRun)
        {
            foreach (Gallery.Owner owner in gallery.Owners.Values)
            {
                await SaveOwner(storageAccount, owner, isDryRun);
            }
        }

        private static async Task SaveOwner(CloudStorageAccount storageAccount, Gallery.Owner owner, bool isDryRun)
        {
            try
            {
                JToken root = owner.CreateDocument();

                // this should never be null but we can protect ourselves from problems in the data or bugs in our SQL
                if (root != null)
                {
                    if (!isDryRun)
                    {
                        await CreateBlob(storageAccount, "apiv3", owner.Name, "application/json", root.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(owner.UserName, e);
            }
        }

        private static async Task SavePackages(CloudStorageAccount storageAccount, Gallery gallery, bool isDryRun)
        {
            foreach (Gallery.Package package in gallery.Packages.Values)
            {
                await SavePackage(storageAccount, package, isDryRun);
            }
        }

        private static async Task SavePackage(CloudStorageAccount storageAccount, Gallery.Package package, bool isDryRun)
        {
            try
            {
                JToken root = package.CreateDocument();

                // this should never be null but we can protect ourselves from problems in the data or bugs in our SQL
                if (root != null)
                {
                    if (!isDryRun)
                    {
                        await CreateBlob(storageAccount, "apiv3", package.Name, "application/json", root.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(package.PackageRegistration.Id + "." + package.Version, e);
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
    }
}
