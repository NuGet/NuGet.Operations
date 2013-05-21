using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.IO.Compression;

namespace ApiProto.Controllers
{
    public class ApiController : Controller
    {
        IStorage _storage;
        IStorage _enumeration;
        ISearch _search;

        public ApiController()
        {
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
            string storageContainer = ConfigurationManager.AppSettings["StorageContainer"];
            string enumerationContainer = ConfigurationManager.AppSettings["EnumerationContainer"];
            string searchContainer = ConfigurationManager.AppSettings["SearchContainer"];

            _storage = new AzureBlobStorage(connectionString, storageContainer);
            _enumeration = new AzureBlobStorage(connectionString, enumerationContainer);
            _search = new SearchService();
        }

        //
        // GET: api/v3/package/{id}

        public async Task<ActionResult> PackageRegistration(string id)
        {
            id = id.ToLowerInvariant();

            string json = await _storage.Load("package/" + id);

            JObject content = JObject.Parse(json);
            ExpandURIs(Url.RequestContext.HttpContext.Request, content);
            return MakeContentResult(content);
        }

        //
        // GET: api/v3/package/{id}/{version}

        public async Task<ActionResult> Package(string id, string version)
        {
            id = id.ToLowerInvariant();
            version = version.ToLowerInvariant();

            string json = await _storage.Load("package/" + id + "/" + version);

            JObject content = JObject.Parse(json);
            ExpandURIs(Url.RequestContext.HttpContext.Request, content);
            return MakeContentResult(content);
        }

        //
        // GET: api/v3/owner/{username}

        public async Task<ActionResult> Owner(string username)
        {
            username = username.ToLowerInvariant();

            string json = await _storage.Load("owner/" + username);
            JObject content = JObject.Parse(json);
            ExpandURIs(Url.RequestContext.HttpContext.Request, content);

            return MakeContentResult(content);
        }

        //
        // GET: api/v3/packagelist/{page}

        public async Task<ActionResult> PackageList(string page)
        {
            page = page.ToLowerInvariant();

            string json = await _enumeration.Load("packagelist/" + page);
            JToken content = JToken.Parse(json);

            if (page == "index")
            {
                IndexPageExpandURIs(Url.RequestContext.HttpContext.Request, content);
            }
            else
            {
                ExpandURIs(Url.RequestContext.HttpContext.Request, content);
            }

            return MakeContentResult(content);
        }

        //
        // GET: api/v3/packagebydate/{page}

        public async Task<ActionResult> PackageByDate(string page)
        {
            page = page.ToLowerInvariant();

            string json = await _enumeration.Load("packagebydate/" + page);
            JToken content = JToken.Parse(json);

            if (page == "index")
            {
                IndexPageExpandURIs(Url.RequestContext.HttpContext.Request, content);
            }
            else
            {
                ExpandURIs(Url.RequestContext.HttpContext.Request, content);
            }

            return MakeContentResult(content);
        }

        //
        // GET: api/v3/search?q=term&targetFramework=4

        public async Task<ActionResult> Search()
        {
            NameValueCollection queryString = this.Request.QueryString;

            string term = queryString["q"];

            IList<SearchResult> results = await _search.Query(term);

            JToken content = CreateContentFromSearchResults(results);
            ExpandURIs(Url.RequestContext.HttpContext.Request, content);
            return MakeContentResult(content);
        }

        //
        // GET: api/v3/packagecontent

        public async Task<ActionResult> PackageContent(string id, string version)
        {
            id = id.ToLowerInvariant();
            version = version.ToLowerInvariant();

            JToken content = await Load(id, version);

            return MakeContentResult(content);
        }

        //  private helper functions

        async static Task<JToken> Load(string id, string version)
        {
            //TEMP
            string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=nugetgalleryqa;AccountKey=OfOH3lvBTH2XHHH9Xq77rO/XEvZidKjOmeaVnWrUvEW92cQ6fqLG+4S4s82LpYlQhrQ5ofVAB2u87H6dRsxdmw==";
            string Container = "packages";

            string name = string.Format("{0}.{1}.nupkg", id, version);

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(Container);
            CloudBlockBlob blockBlob = blobContainer.GetBlockBlobReference(name);

            MemoryStream stream = new MemoryStream();

            await Task.Factory.FromAsync(blockBlob.BeginDownloadToStream(stream, null, null), blockBlob.EndDownloadToStream);

            stream.Seek(0, SeekOrigin.Begin);

            JArray array = new JArray();

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry item in archive.Entries)
                {
                    array.Add(item.FullName);
                }
            }

            return array;
        }

        private JToken CreateContentFromSearchResults(IList<SearchResult> results)
        {
            JArray array = new JArray();
            foreach (SearchResult result in results)
            {
                array.Add(result.Details);
            }
            return array;
        }

        private static ContentResult MakeContentResult(JToken content)
        {
            return new ContentResult
            {
                Content = content.ToString(),
                ContentType = "application/json"
            };
        }

        private static void IndexPageExpandURIs(HttpRequestBase request, JToken content)
        {
            string protocol = request.IsSecureConnection ? "https" : "http";
            string host = request.Url.Host;
            int port = request.Url.Port;

            foreach (JValue value in (JArray)content)
            {
                value.Value = string.Format("{0}://{1}:{2}/api/v3/{3}/", protocol, host, port, value.Value);
            }
        }

        private static void ExpandURIs(HttpRequestBase request, JToken content)
        {
            string protocol = request.IsSecureConnection ? "https" : "http";
            string host = request.Url.Host;
            int port = request.Url.Port;

            Func<string, string> CreateUri = (value) => { return string.Format("{0}://{1}:{2}/api/v3/{3}/", protocol, host, port, value); };

            ProcessContent(content, CreateUri);
        }

        private static void ProcessContent(JToken parent, Func<string, string> createUri)
        {
            foreach (JToken child in parent)
            {
                if (child is JProperty)
                {
                    JProperty property = (JProperty)child;

                    if (property.Name == "uri" || property.Name == "prev" || property.Name == "next")
                    {
                        string value = property.Value.ToString();
                        if (value != string.Empty)
                        {
                            property.Value = createUri(value);
                        }
                    }
                    else if (!(property.Value is JValue))
                    {
                        ProcessContent(property.Value, createUri);
                    }
                }
                else
                {
                    ProcessContent(child, createUri);
                }
            }
        }

        private static JObject NameValueCollection2Json(NameValueCollection nameValueCollection)
        {
            JObject obj = new JObject();
            foreach (string name in nameValueCollection.AllKeys)
            {
                JArray values = new JArray();
                foreach (string value in nameValueCollection.GetValues(name))
                {
                    values.Add(value);
                }
                obj.Add(name, values);
            }
            return obj;
        }
    }
}
