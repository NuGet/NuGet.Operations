using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using SimpleBatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RenderBlobs
{
    class Program
    {
        static void PrintSummary(Gallery gallery)
        {
            Console.WriteLine("PackageRegistrations: {0}", gallery.PackageRegistrations.Count);
            Console.WriteLine("Owners: {0}", gallery.Owners.Count);
            Console.WriteLine("Packages: {0}", gallery.Packages.Count);
        }

        public static Gallery LoadGallery([Config("SqlBlobConfig.txt")] SqlBlobConfig config)
        {
            DateTime start = DateTime.Now;

            Gallery gallery = SqlGallery.Load(config.sqlConnectionString);

            PrintSummary(gallery);

            Console.WriteLine("Load sql time: {0} minutes", (DateTime.Now - start).TotalMinutes);
            Console.WriteLine("Total memory: {0}", GC.GetTotalMemory(true));

            return gallery;
        }

        static void RenderGallery(SqlBlobConfig config, Gallery gallery)
        {
            DateTime start = DateTime.Now;

            StorageGallery.Save(config.storageConnectionString, gallery, isDryRun: false).Wait();

            Console.WriteLine("Save blobs time: {0} minutes", (DateTime.Now - start).TotalMinutes);
        }

        static void IndexGallery(SqlBlobConfig config, Gallery gallery)
        {
            DateTime start = DateTime.Now;

            LuceneGallery.Save(config.storageConnectionString, gallery, config.warehouseSqlConnectionString).Wait();

            Console.WriteLine("Create index time: {0} minutes", (DateTime.Now - start).TotalMinutes);
        }

        static void RenderEnum(StorageAddress storage, Gallery gallery)
        {
            DateTime start = DateTime.Now;

            StorageEnumerations.Save(storage, gallery).Wait();

            Console.WriteLine("Save blobs time: {0} minutes", (DateTime.Now - start).TotalMinutes);
        }

        static string Indent(int depth)
        {
            string result = string.Empty;
            while (depth-- > 0)
            {
                result += "  ";
            }
            return result;
        }

        static void Print(Exception e, TextWriter output, int depth)
        {
            if (e != null)
            {
                output.WriteLine("{0}{1} {2}", Indent(depth), e.GetType().Name, e.Message);
                output.WriteLine("{0}{1}", Indent(depth), e.StackTrace);
                if (e is AggregateException)
                {
                    foreach (Exception innerException in ((AggregateException)e).InnerExceptions)
                    {
                        Print(innerException, output, depth + 1);
                    }
                }
                else
                {
                    Print(e.InnerException, output, depth + 1);
                }
            }
        }

        [NoAutomaticTrigger]
        public static void LoadAndRender([Config("SqlBlobConfig.txt")] SqlBlobConfig config)
        {
            Gallery gallery = LoadGallery(config);

            RenderGallery(config, gallery);
        }

        [NoAutomaticTrigger]
        public static void LoadAndIndexGallery([Config("SqlBlobConfig.txt")] SqlBlobConfig config)
        {
            Gallery gallery = LoadGallery(config);

            IndexGallery(config, gallery);
        }

        [NoAutomaticTrigger]
        public static void RenderPackageContent([Config("SqlBlobConfig.txt")] SqlBlobConfig config)
        {
            PackageExplorer.RenderPackageContent(config.packageConnectionString, config.storageConnectionString).Wait();
        }

        [NoAutomaticTrigger]
        public static void RenderSingleBlob(
            [BlobInput(@"packages\{name}.nupkg")] Stream input,
            [BlobOutput(@"packagecontent\{name}.json")] TextWriter output,
            string name
            )
        {
            try
            {
                Console.WriteLine("{0}", name);
                PackageExplorer.RenderSinglePackageContent(input, output);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0} Exception: {1}", name, e.Message), e);
            }
        }

        [NoAutomaticTrigger]
        public static void Run(ICall call, [Config("SqlBlobConfig.txt")] SqlBlobConfig config)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(config.packageConnectionString);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference("packages");

            foreach (CloudBlockBlob item in blobContainer.ListBlobs(useFlatBlobListing: true))
            {
                string name = item.Name.Substring(0, item.Name.Length - 6);

                Console.WriteLine(name);

                //call.QueueCall("RenderSingleBlob", new { name = name });
            }
        }

        static void Main(string[] args)
        {
            try
            {
                SqlBlobConfig config = JsonConvert.DeserializeObject<SqlBlobConfig>(File.ReadAllText(@"SqlBlobConfig.txt"));

                //LoadAndRender(config);
                //LoadAndIndexGallery(config);
                //RenderPackageContent(config);
            }
            catch (Exception e)
            {
                Print(e, Console.Out, 0);
            }
        }

        public class SqlBlobConfig
        {
            public string sqlConnectionString { get; set; }
            public string warehouseSqlConnectionString { get; set; }
            public string storageConnectionString { get; set; }
            public string packageConnectionString { get; set; }
        }
    }
}
