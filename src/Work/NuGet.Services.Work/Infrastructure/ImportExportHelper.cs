// Adapted from NuGetGallery.sln which is adapted from http://code.msdn.microsoft.com/windowsazure/Windows-Azure-SQL-Database-5eb17fe2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;
using System.Web;
using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using NuGet.Services.Work.DACWebService;
using System.Diagnostics.Tracing;
using NuGet.Services.Work.Jobs;

namespace WASDImportExport
{
    class ImportExportHelper
    {
        public string EndPointUri { get; set; }
        public string StorageKey { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public ImportExportHelper()
        {
            EndPointUri = "";
            ServerName = "";
            StorageKey = "";
            DatabaseName = "";
            UserName = "";
            Password = "";
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public string DoExport(ExportDatabaseEventSource log, string blobUri, bool whatIf)
        {
            string requestGuid = null;

            //Setup Web Request for Export Operation
            WebRequest webRequest = WebRequest.Create(this.EndPointUri + @"/Export");
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.ContentType = @"application/xml";

            //Create Web Request Inputs - Blob Storage Credentials and Server Connection Info
            ExportInput exportInputs = new ExportInput
            {
                BlobCredentials = new BlobStorageAccessKeyCredentials
                {
                    StorageAccessKey = this.StorageKey,
                    Uri = String.Format(blobUri, this.DatabaseName, DateTime.UtcNow.Ticks.ToString())
                },
                ConnectionInfo = new ConnectionInfo
                {
                    ServerName = this.ServerName,
                    DatabaseName = this.DatabaseName,
                    UserName = this.UserName,
                    Password = this.Password
                }
            };

            //Perform Web Request
            DataContractSerializer dataContractSerializer = new DataContractSerializer(exportInputs.GetType());
            log.RequestUri(webRequest.RequestUri.AbsoluteUri);
            if (whatIf)
            {
                using (var strm = new MemoryStream())
                {
                    dataContractSerializer.WriteObject(strm, exportInputs);
                    strm.Flush();
                    strm.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(strm))
                    {
                        log.WouldHaveSent(reader.ReadToEnd());
                    }
                }
                return null;
            }
            else
            {
                using (var strm = new MemoryStream())
                {
                    dataContractSerializer.WriteObject(strm, exportInputs);
                    strm.Flush();
                    strm.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(strm))
                    {
                        log.SendingRequest(reader.ReadToEnd());
                    }
                }

                Stream webRequestStream = webRequest.GetRequestStream();
                dataContractSerializer.WriteObject(webRequestStream, exportInputs);
                webRequestStream.Close();

                //Get Response and Extract Request Identifier
                WebResponse webResponse = null;
                XmlReader xmlStreamReader = null;

                try
                {
                    //Initialize the WebResponse to the response from the WebRequest
                    webResponse = webRequest.GetResponse();

                    xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
                    xmlStreamReader.ReadToFollowing("guid");
                    requestGuid = xmlStreamReader.ReadElementContentAsString();
                    return requestGuid;
                }
                catch (WebException responseException)
                {
                    log.RequestFailed(responseException.Message);
                    if (responseException.Response != null)
                    {
                        log.ErrorStatusCode((int)(((HttpWebResponse)responseException.Response).StatusCode));
                        log.ErrorStatusDescription(((HttpWebResponse)responseException.Response).StatusDescription);
                    }
                    return null;
                }
            }
        }

        public string DoImport(ImportDatabaseEventSource log, string blobUri, bool whatIf, int databaseSizeInGB = 5)
        {
            string requestGuid = null;

            //Setup Web Request for Import Operation
            WebRequest webRequest = WebRequest.Create(this.EndPointUri + @"/Import");
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.ContentType = @"application/xml";

            //Create Web Request Inputs - Database Size & Edition, Blob Store Credentials and Server Connection Info
            ImportInput importInputs = new ImportInput
            {
                AzureEdition = "Web",
                DatabaseSizeInGB = databaseSizeInGB,
                BlobCredentials = new BlobStorageAccessKeyCredentials
                {
                    StorageAccessKey = this.StorageKey,
                    Uri = String.Format(blobUri, this.DatabaseName, DateTime.UtcNow.Ticks.ToString())
                },
                ConnectionInfo = new ConnectionInfo
                {
                    ServerName = this.ServerName,
                    DatabaseName = this.DatabaseName,
                    UserName = this.UserName,
                    Password = this.Password
                }
            };

            //Perform Web Request
            Stream webRequestStream = webRequest.GetRequestStream();
            DataContractSerializer dataContractSerializer = new DataContractSerializer(importInputs.GetType());

            if (whatIf)
            {
                using (var strm = new MemoryStream())
                {
                    dataContractSerializer.WriteObject(strm, importInputs);
                    strm.Flush();
                    strm.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(strm))
                    {
                        log.WouldHaveSent(reader.ReadToEnd());
                    }
                }
                return null;
            }
            else
            {
                using (var strm = new MemoryStream())
                {
                    dataContractSerializer.WriteObject(strm, importInputs);
                    strm.Flush();
                    strm.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(strm))
                    {
                        log.SendingRequest(reader.ReadToEnd());
                    }
                }

                dataContractSerializer.WriteObject(webRequestStream, importInputs);
                webRequestStream.Close();

                //Get Response and Extract Request Identifier
                WebResponse webResponse = null;
                XmlReader xmlStreamReader = null;

                try
                {
                    //Initialize the WebResponse to the response from the WebRequest
                    webResponse = webRequest.GetResponse();

                    xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
                    xmlStreamReader.ReadToFollowing("guid");
                    requestGuid = xmlStreamReader.ReadElementContentAsString();
                    return requestGuid;
                }
                catch (WebException responseException)
                {
                    log.RequestFailed(responseException.Message);
                    {
                        log.ErrorStatusCode((int)(((HttpWebResponse)responseException.Response).StatusCode));
                        log.ErrorStatusDescription(((HttpWebResponse)responseException.Response).StatusDescription);
                    }

                    return null;
                }
            }
        }

        public List<StatusInfo> CheckRequestStatus(string requestGuid)
        {
            WebRequest webRequest = WebRequest.Create(this.EndPointUri + string.Format("/Status?servername={0}&username={1}&password={2}&reqId={3}",
                    HttpUtility.UrlEncode(this.ServerName),
                    HttpUtility.UrlEncode(this.UserName),
                    HttpUtility.UrlEncode(this.Password),
                    HttpUtility.UrlEncode(requestGuid)));

            webRequest.Method = WebRequestMethods.Http.Get;
            webRequest.ContentType = @"application/xml";
            WebResponse webResponse = webRequest.GetResponse();
            XmlReader xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
            DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(List<StatusInfo>));

            return (List<StatusInfo>)dataContractSerializer.ReadObject(xmlStreamReader, true);
        }
    }
}
