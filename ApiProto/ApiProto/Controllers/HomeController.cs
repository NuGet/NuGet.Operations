using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.WindowsAzure.Storage;

namespace ApiProto
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            HomeModel model = new HomeModel();

            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            model.BlobEndpoint = storageAccount.BlobEndpoint.ToString();
            model.Container = ConfigurationManager.AppSettings["StorageContainer"];

            return View(model);
        }

    }
}
