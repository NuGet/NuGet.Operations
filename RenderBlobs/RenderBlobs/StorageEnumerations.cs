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
    static class StorageEnumerations
    {
        public static async Task Save(StorageAddress storage, Gallery gallery)
        {
            await PackageRegistrationEnumeration(storage, gallery);
            await PackageEnumeration(storage, gallery);
        }

        private static async Task PackageRegistrationEnumeration(StorageAddress storage, Gallery gallery)
        {
            IList<string> indexPage = await SavePackageRegistrationEnumeration(storage, gallery, 100);
            await CreateEnumerationIndexPage(storage, "packagelist", indexPage);
        }

        private static async Task<IList<string>> SavePackageRegistrationEnumeration(StorageAddress storage, Gallery gallery, int size)
        {
            List<string> indexPage = new List<string>();

            int pageIndex = 1;
            int current = 0;

            IList<Gallery.PackageRegistration> currentPage = new List<Gallery.PackageRegistration>(); 

            foreach (Gallery.PackageRegistration packageRegistration in gallery.PackageRegistrations.Values)
            {
                current++;

                currentPage.Add(packageRegistration);

                //  if we equal a multiple of the size then its time to cut another page

                if (current == pageIndex * size)
                {
                    int? prev = (pageIndex != 1) ? pageIndex - 1 : (int?)null;
                    int? next = (gallery.PackageRegistrations.Values.Count - (pageIndex * size) > 0) ? pageIndex + 1 : (int?)null;

                    string name = await MakePage(storage, currentPage, pageIndex, prev, next);

                    indexPage.Add(name);

                    currentPage = new List<Gallery.PackageRegistration>();
                    pageIndex++;
                }
            }

            //  part-filled-page - if we have one it is always the last but it might also be the first

            if (currentPage.Count > 0)
            {
                int? prev = (pageIndex != 1) ? pageIndex - 1 : (int?)null;
                int? next = null;

                string name = await MakePage(storage, currentPage, pageIndex, prev, next);

                indexPage.Add(name);
            }

            return indexPage;
        }

        private async static Task<string> MakePage(StorageAddress storage, IList<Gallery.PackageRegistration> currentPage, int page, int? prev, int? next)
        {
            JArray packages = new JArray();

            foreach (Gallery.PackageRegistration packageRegistration in currentPage)
            {
                JToken package = packageRegistration.CreateDocument();
                packages.Add(package);
            }

            JObject root = new JObject();
            root.Add("packages", packages);

            root.Add("prev", CreateName("packagelist", prev));
            root.Add("next", CreateName("packagelist", next));

            string name = string.Format("packagelist/page{0}", page);

            await Utils.CreateBlob(storage, name, "application/json", root.ToString());

            return name;
        }

        private static string CreateName(string enumerationName, int? pageNumber)
        {
            return (pageNumber == null) ? null : string.Format("{0}/page{1}", enumerationName, pageNumber);
        }

        private async static Task CreateEnumerationIndexPage(StorageAddress storage, string enumerationName, IList<string> indexPage)
        {
            JArray pages = new JArray();
            
            foreach (string page in indexPage)
            {
                pages.Add(page);
            }

            await Utils.CreateBlob(storage, enumerationName + "/index", "application/json", pages.ToString());
        }

        private static async Task PackageEnumeration(StorageAddress storage, Gallery gallery)
        {
            IList<string> indexPage = await SavePackageEnumeration(storage, gallery, 100);
            await CreateEnumerationIndexPage(storage, "packagebydate", indexPage);
        }

        private static async Task<IList<string>> SavePackageEnumeration(StorageAddress storage, Gallery gallery, int size)
        {
            List<string> indexPage = new List<string>();

            List<Tuple<DateTime, string>> byDateList = new List<Tuple<DateTime, string>>();

            foreach (Gallery.Package package in gallery.Packages.Values)
            {
                byDateList.Add(new Tuple<DateTime, string>(package.Published, package.PackageRegistration.Id + "/" + package.Version));
            }

            byDateList.Sort((x, y) => x.Item1.CompareTo(y.Item1));

            int pageIndex = 1;
            int current = 0;

            IList<Gallery.Package> currentPage = new List<Gallery.Package>();

            foreach (Tuple<DateTime, string> item in byDateList)
            {
                Gallery.Package package = gallery.Packages[item.Item2];

                current++;

                currentPage.Add(package);

                //  if we equal a multiple of the size then its time to cut another page

                if (current == pageIndex * size)
                {
                    int? prev = (pageIndex != 1) ? pageIndex - 1 : (int?)null;
                    int? next = (gallery.PackageRegistrations.Values.Count - (pageIndex * size) > 0) ? pageIndex + 1 : (int?)null;

                    string name = await MakePackagePage(storage, currentPage, pageIndex, prev, next);

                    indexPage.Add(name);

                    currentPage = new List<Gallery.Package>();
                    pageIndex++;
                }
            }

            if (currentPage.Count > 0)
            {
                int? prev = (pageIndex != 1) ? pageIndex - 1 : (int?)null;
                int? next = null;

                string name = await MakePackagePage(storage, currentPage, pageIndex, prev, next);

                indexPage.Add(name);
            }

            return indexPage;
        }

        private async static Task<string> MakePackagePage(StorageAddress storage, IList<Gallery.Package> currentPage, int page, int? prev, int? next)
        {
            JArray packages = new JArray();

            foreach (Gallery.Package package in currentPage)
            {
                JToken p = package.CreateDocument();
                packages.Add(p);
            }

            JObject root = new JObject();
            root.Add("packages", packages);

            root.Add("prev", CreateName("packagebydate", prev));
            root.Add("next", CreateName("packagebydate", next));

            string name = string.Format("packagebydate/page{0}", page);

            await Utils.CreateBlob(storage, name, "application/json", root.ToString());

            return name;
        }
    }
}
